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
using Application.Reservations.ChangeDesk;
using Application.Users.Login;
using Application.Users.Register;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Users;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.Authentication;
using MediatR;
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
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.Admin, policy => policy.RequireClaim(ClaimTypes.Role, PolicyNames.Admin));
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsConfiguration.Customize);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGenWithAuth();

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

        Assert.Equal(11, document.Paths.Values.Sum(path => path.Operations?.Count ?? 0));
        Assert.Contains("/api/login", document.Paths.Keys, StringComparer.Ordinal);
        Assert.Contains("/api/locations", document.Paths.Keys, StringComparer.Ordinal);
        Assert.Contains("/api/locations/{locationId}/desks", document.Paths.Keys, StringComparer.Ordinal);
        Assert.Contains("/api/reservations", document.Paths.Keys, StringComparer.Ordinal);
    }

    [Fact]
    public async Task DuplicateRegistration_ReturnsConflictProblem()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailAlreadyExistsException("user@example.com"));

        using var response = await _client.PostAsJsonAsync("/api/register",
            new { Email = "user@example.com", FirstName = "Test", LastName = "User", Password = "password" },
            TestContext.Current.CancellationToken);

        await AssertProblemResponse(response, HttpStatusCode.Conflict, "POST /api/register", "15.5.10");
        _sender.VerifyAll();
    }

    [Fact]
    public async Task UnexpectedFailure_ReturnsGenericServerProblem()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Secret database connection"));

        using var response = await _client.PostAsJsonAsync("/api/login",
            new { Email = "user@example.com", Password = "password" }, TestContext.Current.CancellationToken);

        await AssertProblemResponse(response, HttpStatusCode.InternalServerError, "POST /api/login", "15.6.1");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("Secret", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("GET", "/api/missing-route", HttpStatusCode.NotFound, "15.5.5")]
    [InlineData("GET", "/api/login", HttpStatusCode.MethodNotAllowed, "15.5.6")]
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
        using var response = await _client.PostAsync("/api/login", content, TestContext.Current.CancellationToken);

        await AssertProblemResponse(response, status, "POST /api/login", section);
        Assert.Empty(_sender.Invocations);
    }

    [Theory]
    [InlineData("/api/register", "POST", "200,400,404,409,413,415,500")]
    [InlineData("/api/login", "POST", "200,400,413,415,500")]
    [InlineData("/api/locations", "POST", "201,400,401,403,409,413,415,500")]
    [InlineData("/api/locations/{id}", "DELETE", "204,400,401,403,404,500")]
    [InlineData("/api/locations/{locationId}/desks", "GET", "200,400,401,500")]
    [InlineData("/api/locations/{locationId}/desks", "POST", "201,400,401,403,404,409,413,415,500")]
    [InlineData("/api/locations/{locationId}/desks/{id}", "GET", "200,400,401,404,500")]
    [InlineData("/api/locations/{locationId}/desks/{id}", "PUT", "204,400,401,403,404,500")]
    [InlineData("/api/locations/{locationId}/desks/{deskId}", "DELETE", "204,400,401,403,404,500")]
    [InlineData("/api/reservations", "POST", "201,400,401,404,409,413,415,500")]
    [InlineData("/api/reservations/{id}/change-desk", "PUT", "204,400,401,403,404,413,415,500")]
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
    public async Task InvalidLogin_ReturnsValidationProblemWithBadRequestStatus()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException([new ValidationFailure("Email", "Email is invalid.")]));

        using var response = await _client.PostAsJsonAsync("/api/login",
            new { Email = "invalid", Password = "password" }, TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Email is invalid.", problem.GetProperty("errors").GetProperty("Email")[0].GetString());
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

    [Fact]
    public async Task Login_AllowsAnonymousAndCreatesApplicationCommand()
    {
        _client.DefaultRequestHeaders.Remove("Test-Role");
        _sender.Setup(sender => sender.Send(new LoginUserCommand("user@example.com", "password"),
            It.IsAny<CancellationToken>())).ReturnsAsync("token");

        using var response = await _client.PostAsJsonAsync("/api/login",
            new { Email = "user@example.com", Password = "password" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("token", await response.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken));
        _sender.VerifyAll();
    }

    [Theory]
    [InlineData("GET", "/api/locations/11111111-1111-1111-1111-111111111111/desks", null, HttpStatusCode.Unauthorized)]
    [InlineData("POST", "/api/locations", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("POST", "/api/locations/11111111-1111-1111-1111-111111111111/desks", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("DELETE", "/api/locations/11111111-1111-1111-1111-111111111111/desks/22222222-2222-2222-2222-222222222222", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("POST", "/api/reservations", null, HttpStatusCode.Unauthorized)]
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
