using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Application.Desks.ChangeAvailability;
using Application.Desks.Create;
using Application.Desks.GetDetails;
using Application.Desks.GetPagedByLocation;
using Application.Dtos;
using Application.Interfaces;
using Application.Locations.Update;
using Application.Reservations.Cancel;
using Application.Reservations.ChangeDesk;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Locations;
using Domain.Exceptions.Reservations;
using Domain.Locations;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.Authentication;
using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Swashbuckle.AspNetCore.Swagger;
using Web.Api.Endpoints;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace UnitTests.Endpoints;

public sealed class EndpointTests : IAsyncLifetime
{
    private readonly Mock<ISender> _sender = new(MockBehavior.Strict);

    private readonly WebApplication _app;
    private readonly HttpClient _client = new();

    public EndpointTests()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(_sender.Object);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext>(_ => new Mock<IUserContext>(MockBehavior.Strict).Object);


        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.Admin, policy => policy.RequireClaim(ClaimTypes.Role, PolicyNames.Admin));
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsConfiguration.Customize);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGenWithAuth(builder.Configuration);

        _app = builder.Build();
        _app.UseExceptionHandler();
        _app.UseStatusCodePages();
        _app.UseAuthentication();
        _app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("Test-Role", out var role))
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, role.ToString())], "Test"));
            }

            await next(context);
        });
        _app.UseAuthorization();

        _app.MapAuthEndpoints();
        _app.MapLocationEndpoints();
        _app.MapDeskEndpoints();
        _app.MapReservationEndpoints();
        _client.DefaultRequestHeaders.Add("Test-Role", PolicyNames.Admin);
    }

    public async ValueTask InitializeAsync()
    {
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client.BaseAddress = new Uri(_app.Urls.Single());
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public void Swagger_DocumentsAllGroupedEndpoints()
    {
        var document = _app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        Assert.Equal(12, document.Paths.Values.Sum(path => path.Operations?.Count ?? 0));
        Assert.Contains("/api/profile", document.Paths.Keys, StringComparer.Ordinal);
        Assert.Contains("/api/locations", document.Paths.Keys, StringComparer.Ordinal);
        Assert.Contains("/api/locations/{locationId}/desks", document.Paths.Keys, StringComparer.Ordinal);
        Assert.Contains("/api/reservations", document.Paths.Keys, StringComparer.Ordinal);
        var cancel = Assert.Single(document.Paths["/api/reservations/{id}/cancel"].Operations!).Value;
        foreach (var status in new[] { "204", "401", "403", "404", "409" })
            Assert.Contains(status, cancel.Responses!.Keys, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("Administrator")]
    [InlineData("Employee")]
    public async Task Cancel_DispatchesRouteIdAndReturnsNoContent(string role)
    {
        var id = Guid.NewGuid();
        _client.DefaultRequestHeaders.Remove("Test-Role");
        _client.DefaultRequestHeaders.Add("Test-Role", role);
        _sender.Setup(s => s.Send(new CancelReservationCommand(id), It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);
        using var response = await _client.PutAsync($"/api/reservations/{id}/cancel", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _sender.Verify(s => s.Send(new CancelReservationCommand(id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_AnonymousIsUnauthorized()
    {
        _client.DefaultRequestHeaders.Remove("Test-Role");
        var path = $"/api/reservations/{Guid.NewGuid()}/cancel";
        using var response = await _client.PutAsync(path, null, TestContext.Current.CancellationToken);
        await AssertProblemResponse(response, HttpStatusCode.Unauthorized, $"PUT {path}", "15.5.2");
        Assert.Empty(_sender.Invocations);
    }

    [Theory]
    [InlineData(403, "15.5.4")]
    [InlineData(404, "15.5.5")]
    [InlineData(409, "15.5.10")]
    public async Task Cancel_ReturnsDocumentedProblems(int status, string section)
    {
        var id = Guid.NewGuid();
        Exception exception = status switch
        {
            403 => new UnauthorizedReservationChangeException(id, Guid.NewGuid()),
            404 => new ReservationNotFoundException(id),
            _ => new ReservationConflictException(id)
        };
        _sender.Setup(s => s.Send(new CancelReservationCommand(id), It.IsAny<CancellationToken>())).ThrowsAsync(exception);
        var path = $"/api/reservations/{id}/cancel";
        using var response = await _client.PutAsync(path, null, TestContext.Current.CancellationToken);
        await AssertProblemResponse(response, (HttpStatusCode)status, $"PUT {path}", section);
    }

    [Fact]
    public async Task UnexpectedFailure_ReturnsGenericServerProblem()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<CreateDeskCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Secret database connection"));

        using var response = await _client.PostAsJsonAsync("/api/locations/11111111-1111-1111-1111-111111111111/desks",
            new { Name = "Desk" }, TestContext.Current.CancellationToken);

        await AssertProblemResponse(response, HttpStatusCode.InternalServerError, "POST /api/locations/11111111-1111-1111-1111-111111111111/desks", "15.6.1");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("Secret", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("GET", "/api/missing-route", HttpStatusCode.NotFound, "15.5.5")]
    [InlineData("GET", "/api/locations/11111111-1111-1111-1111-111111111111/desks?page=invalid", HttpStatusCode.BadRequest, "15.5.1")]
    public async Task EmptyErrors_ReturnProblemDetails(string method, string path, HttpStatusCode status, string section)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Accept.ParseAdd("application/problem+json");
        using var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        await AssertProblemResponse(response, status, $"{method} {path.Split('?')[0]}", section);
        Assert.Empty(_sender.Invocations);
    }

    [Theory]
    [InlineData("application/json", "{", HttpStatusCode.BadRequest, "15.5.1")]
    [InlineData("text/plain", "not-json", HttpStatusCode.UnsupportedMediaType, "15.5.16")]
    public async Task InvalidBody_ReturnsProblemDetails(string contentType, string body, HttpStatusCode status, string section)
    {
        using var content = new StringContent(body, Encoding.UTF8, contentType);
        using var response = await _client.PostAsync("/api/locations/11111111-1111-1111-1111-111111111111/desks", content, TestContext.Current.CancellationToken);

        await AssertProblemResponse(response, status, "POST /api/locations/11111111-1111-1111-1111-111111111111/desks", section);
        Assert.Empty(_sender.Invocations);
    }

    [Theory]
    [InlineData("/api/profile", "GET", "200,401,500")]

    [InlineData("/api/locations", "POST", "201,400,401,403,409,413,415,500")]
    [InlineData("/api/locations/{id}", "PUT", "200,400,401,403,404,409,413,415,500")]
    [InlineData("/api/locations/{id}", "DELETE", "204,400,401,403,404,500")]
    [InlineData("/api/locations/{locationId}/desks", "GET", "200,400,401,500")]
    [InlineData("/api/locations/{locationId}/desks", "POST", "201,400,401,403,404,409,413,415,500")]
    [InlineData("/api/locations/{locationId}/desks/{id}", "GET", "200,400,401,404,500")]
    [InlineData("/api/locations/{locationId}/desks/{id}", "PUT", "204,400,401,403,404,500")]
    [InlineData("/api/locations/{locationId}/desks/{deskId}", "DELETE", "204,400,401,403,404,409,500")]
    [InlineData("/api/reservations", "POST", "201,400,401,404,409,413,415,500")]
    [InlineData("/api/reservations/{id}/change-desk", "PUT", "204,400,401,403,404,409,413,415,500")]
    [InlineData("/api/reservations/{id}/cancel", "PUT", "204,400,401,403,404,409,413,415,500")]
    public void Swagger_DocumentsResponseCodesAndProblemSchemas(string path, string method, string expectedCodes)
    {
        var document = _app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var operation = document.Paths[path].Operations!
            .Single(pair => string.Equals(pair.Key.ToString(), method, StringComparison.OrdinalIgnoreCase)).Value;
        Assert.NotNull(operation.Responses);
        Assert.Equal(expectedCodes.Split(',').Order(StringComparer.Ordinal),
            operation.Responses.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal);

        foreach (var (status, response) in operation.Responses)
        {
            if (string.CompareOrdinal(status, "400") < 0)
                continue;

            Assert.NotNull(response.Content);
            Assert.Contains("application/problem+json", response.Content.Keys, StringComparer.Ordinal);
            Assert.NotNull(response.Content["application/problem+json"].Schema);
        }
    }

    [Fact]
    public async Task MissingDesk_ReturnsProblemDetailsThroughExceptionMiddleware()
    {
        var deskId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var exception = new DeskNotFoundException(deskId);
        _sender.Setup(sender => sender.Send(new GetDeskDetailsQuery(deskId, locationId),
            It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        using var response = await _client.GetAsync($"/api/locations/{locationId}/desks/{deskId}",
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Not Found", problem.GetProperty("title").GetString());
        Assert.Equal(exception.Message, problem.GetProperty("detail").GetString());
        Assert.Equal($"GET /api/locations/{locationId}/desks/{deskId}", problem.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        Assert.Equal(DateTimeKind.Utc, problem.GetProperty("timestamp").GetDateTime().Kind);
        _sender.VerifyAll();
    }

    [Fact]
    public async Task InvalidDesk_ReturnsValidationProblemWithBadRequestStatus()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<CreateDeskCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException([new ValidationFailure("Name", "Name is invalid.")]));
        using var response = await _client.PostAsJsonAsync("/api/locations/11111111-1111-1111-1111-111111111111/desks",
            new { Name = "" }, TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Name is invalid.", problem.GetProperty("errors").GetProperty("Name")[0].GetString());
        _sender.VerifyAll();
    }

    [Fact]
    public async Task CreateDesk_UsesRouteLocationAndReturnsDetailsUrl()
    {
        var locationId = Guid.NewGuid();
        var desk = new DeskDto(Guid.NewGuid(), "Window desk", null, locationId, true);
        _sender.Setup(sender => sender.Send(It.Is<CreateDeskCommand>(command =>
                command.LocationId == locationId && command.Name == desk.Name && command.Description == null),
            It.IsAny<CancellationToken>())).ReturnsAsync(desk);

        using var response = await _client.PostAsJsonAsync($"/api/locations/{locationId}/desks",
            new { desk.Name, LocationId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"{_client.BaseAddress}api/locations/{locationId}/desks/{desk.Id}",
            response.Headers.Location?.ToString());
        Assert.Equal(desk, await response.Content.ReadFromJsonAsync<DeskDto>(TestContext.Current.CancellationToken));
        _sender.VerifyAll();
    }

    [Theory]
    [InlineData("")]
    [InlineData("?isAvailable=true&isBookable=false&startDate=2026-10-09&endDate=2026-10-10&page=2&pageSize=5")]
    public async Task GetDesks_BindsOptionalQueryParameters(string queryString)
    {
        var locationId = Guid.NewGuid();
        var hasFilters = queryString.Length > 0;
        var expected = new GetDesksByLocationQuery(locationId,
            new DeskAvailabilityFilter(hasFilters ? true : null, hasFilters ? false : null),
            new DateRange(hasFilters ? new DateOnly(2026, 10, 9) : null, hasFilters ? new DateOnly(2026, 10, 10) : null),
            new PaginationFilter(hasFilters ? 2 : null, hasFilters ? 5 : null));
        _sender.Setup(sender => sender.Send(expected, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedDto<DeskDto>([], 1, 10, 0));

        using var response = await _client.GetAsync($"/api/locations/{locationId}/desks{queryString}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _sender.VerifyAll();
    }

    [Fact]
    public async Task ChangeAvailability_BindsQueryAndReturnsNoContent()
    {
        var locationId = Guid.NewGuid();
        var deskId = Guid.NewGuid();
        _sender.Setup(sender => sender.Send(new ChangeDeskAvailabilityCommand(deskId, locationId, false),
            It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

        using var response = await _client.PutAsync($"/api/locations/{locationId}/desks/{deskId}?isAvailable=false",
            null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _sender.VerifyAll();
    }

    [Fact]
    public async Task ChangeReservedDesk_UsesRouteId()
    {
        var reservationId = Guid.NewGuid();
        var deskId = Guid.NewGuid();
        _sender.Setup(sender => sender.Send(new ChangeDeskCommand(reservationId, deskId),
            It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

        using var response = await _client.PutAsJsonAsync($"/api/reservations/{reservationId}/change-desk",
            new { DeskId = deskId, Id = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _sender.VerifyAll();
    }

    [Theory]
    [InlineData("GET", "/api/locations/11111111-1111-1111-1111-111111111111/desks", null, HttpStatusCode.Unauthorized)]
    [InlineData("POST", "/api/locations", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("PUT", "/api/locations/11111111-1111-1111-1111-111111111111", null, HttpStatusCode.Unauthorized)]
    [InlineData("PUT", "/api/locations/11111111-1111-1111-1111-111111111111", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("POST", "/api/locations/11111111-1111-1111-1111-111111111111/desks", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("DELETE", "/api/locations/11111111-1111-1111-1111-111111111111/desks/22222222-2222-2222-2222-222222222222", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("POST", "/api/reservations", null, HttpStatusCode.Unauthorized)]
    [InlineData("GET", "/api/profile", null, HttpStatusCode.Unauthorized)]
    public async Task ProtectedEndpoints_EnforceAuthorization(string method, string path, string? role, HttpStatusCode status)
    {
        _client.DefaultRequestHeaders.Remove("Test-Role");
        if (role is not null)
        {
            _client.DefaultRequestHeaders.Add("Test-Role", role);
        }

        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        await AssertProblemResponse(response, status, $"{method} {path}",
            status == HttpStatusCode.Unauthorized ? "15.5.2" : "15.5.4");
        if (status == HttpStatusCode.Unauthorized)
            Assert.Contains(response.Headers.WwwAuthenticate, header => string.Equals(header.Scheme, "Bearer", StringComparison.Ordinal));
        Assert.Empty(_sender.Invocations);
    }

    [Fact]
    public async Task UpdateLocation_UsesRouteIdAndReturnsUpdatedDetails()
    {
        var id = Guid.NewGuid();
        var address = new AddressDto("Street", "10", "City", "00-001");
        var result = new LocationDto(id, "Updated", address);
        _sender.Setup(sender => sender.Send(new UpdateLocationCommand(id, result.Name, address), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        using var response = await _client.PutAsJsonAsync($"/api/locations/{id}", new { result.Name, Address = address, Id = Guid.NewGuid() }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(result, await response.Content.ReadFromJsonAsync<LocationDto>(TestContext.Current.CancellationToken));
        _sender.VerifyAll();
    }

    [Theory]
    [InlineData(400, "15.5.1")]
    [InlineData(404, "15.5.5")]
    [InlineData(409, "15.5.10")]
    public async Task UpdateLocation_FailuresReturnProblemDetails(int status, string section)
    {
        var id = Guid.NewGuid();
        Exception exception = status switch
        {
            400 => new ValidationException([new ValidationFailure("Name", "Name is required.")]),
            404 => new LocationNotFoundException(id),
            _ => new LocationAlreadyExistsException("Duplicate", new Address("Street", "1", "City", "Code"))
        };
        _sender.Setup(sender => sender.Send(It.IsAny<UpdateLocationCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(exception);
        using var response = await _client.PutAsJsonAsync($"/api/locations/{id}", new { Name = "Name", Address = new AddressDto("Street", "1", "City", "Code") }, TestContext.Current.CancellationToken);
        await AssertProblemResponse(response, (HttpStatusCode)status, $"PUT /api/locations/{id}", section);
        _sender.VerifyAll();
    }

    [Theory]
    [InlineData("application/json", "{", HttpStatusCode.BadRequest, "15.5.1")]
    [InlineData("text/plain", "text", HttpStatusCode.UnsupportedMediaType, "15.5.16")]
    public async Task UpdateLocation_InvalidBodyDoesNotDispatch(string contentType, string body, HttpStatusCode status, string section)
    {
        var id = Guid.NewGuid();
        using var content = new StringContent(body, Encoding.UTF8, contentType);
        using var response = await _client.PutAsync($"/api/locations/{id}", content, TestContext.Current.CancellationToken);
        await AssertProblemResponse(response, status, $"PUT /api/locations/{id}", section);
        Assert.Empty(_sender.Invocations);
    }

    private static async Task AssertProblemResponse(HttpResponseMessage response, HttpStatusCode status, string instance, string section)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.Equal($"https://tools.ietf.org/html/rfc9110#section-{section}", problem.GetProperty("type").GetString());
        Assert.Equal(instance, problem.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        Assert.Equal(DateTimeKind.Utc, problem.GetProperty("timestamp").GetDateTime().Kind);
    }
}
