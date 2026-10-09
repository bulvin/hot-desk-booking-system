using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Application.Desks.ChangeAvailability;
using Application.Desks.Create;
using Application.Desks.GetDetails;
using Application.Desks.GetPagedByLocation;
using Application.Dtos;
using Application.Reservations.ChangeDesk;
using Application.Users.Login;
using Domain.Exceptions.Desks;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
        var builder = WebApplication.CreateBuilder();
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

        Assert.Equal(status, response.StatusCode);
        Assert.Empty(_sender.Invocations);
    }
}
