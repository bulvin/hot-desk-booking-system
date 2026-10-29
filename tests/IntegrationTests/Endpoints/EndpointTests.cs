using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.Desks.Create;
using Application.Desks.GetDetails;
using Application.Desks.GetPagedByLocation;
using Application.Dtos;
using Application.Locations.Update;
using Application.Reservations.ChangeDesk;
using Domain.Exceptions.Desks;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.Authentication;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Swashbuckle.AspNetCore.Swagger;

namespace IntegrationTests.Endpoints;

public sealed class EndpointTests : IAsyncLifetime
{
    private readonly EndpointTestHost _host = new();
    private Mock<ISender> Sender => _host.Sender;
    private HttpClient Client => _host.Client;

    public ValueTask InitializeAsync() => _host.InitializeAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task UnexpectedFailure_ReturnsGenericServerProblem()
    {
        // Arrange
        Sender.Setup(sender => sender.Send(It.IsAny<CreateDeskCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Secret database connection"));

        // Act
        using var response = await Client.PostAsJsonAsync("/api/locations/11111111-1111-1111-1111-111111111111/desks",
            new { Name = "Desk" }, TestContext.Current.CancellationToken);

        // Assert
        await AssertProblemResponse(response, HttpStatusCode.InternalServerError, "POST /api/locations/11111111-1111-1111-1111-111111111111/desks", "15.6.1");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("Secret", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsCustomizedProblemDetails()
    {
        // Arrange
        const string path = "/api/missing-route";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd("application/problem+json");

        // Act
        using var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await AssertProblemResponse(response, HttpStatusCode.NotFound, $"GET {path}", "15.5.5");
        Assert.Empty(Sender.Invocations);
    }

    [Theory]
    [InlineData("POST", "/api/locations/11111111-1111-1111-1111-111111111111/desks", "application/json", "{", HttpStatusCode.BadRequest, "15.5.1")]
    [InlineData("POST", "/api/locations/11111111-1111-1111-1111-111111111111/desks", "text/plain", "text", HttpStatusCode.UnsupportedMediaType, "15.5.16")]
    [InlineData("PUT", "/api/locations/11111111-1111-1111-111111111111", "text/plain", "text", HttpStatusCode.UnsupportedMediaType, "15.5.16")]
    public async Task InvalidBody_ReturnsProblemDetails(string method, string path, string contentType, string body, HttpStatusCode status, string section)
    {
        // Arrange
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType)
        };

        // Act
        using var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await AssertProblemResponse(response, status, $"{method} {path}", section);
        Assert.Empty(Sender.Invocations);
    }

    [Theory]
    [InlineData("/api/locations/{locationId}/desks", "POST", "201,400,401,403,404,409,413,415,500")]
    [InlineData("/api/reservations/{id}/cancel", "PUT", "204,400,401,403,404,409,413,415,500")]
    public void Swagger_DocumentsResponseCodesAndProblemSchemas(string path, string method, string expectedCodes)
    {
        // Arrange

        // Act
        var document = _host.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var operation = document.Paths[path].Operations!
            .Single(pair => string.Equals(pair.Key.ToString(), method, StringComparison.OrdinalIgnoreCase)).Value;

        // Assert
        Assert.NotNull(operation.Responses);
        Assert.Equal((expectedCodes + ",429").Split(',').Order(StringComparer.Ordinal),
            operation.Responses.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal);
        Assert.NotNull(operation.Responses["429"].Headers);
        Assert.Contains("Retry-After", operation.Responses["429"].Headers!.Keys, StringComparer.Ordinal);

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
        // Arrange
        var deskId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var exception = new DeskNotFoundException(deskId);
        Sender.Setup(sender => sender.Send(new GetDeskDetailsQuery(deskId, locationId),
            It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        // Act
        using var response = await Client.GetAsync($"/api/locations/{locationId}/desks/{deskId}",
            TestContext.Current.CancellationToken);

        // Assert
        var problem = await AssertProblemResponse(response, HttpStatusCode.NotFound,
            $"GET /api/locations/{locationId}/desks/{deskId}", "15.5.5");
        Assert.Equal("Not Found", problem.GetProperty("title").GetString());
        Assert.Equal(exception.Message, problem.GetProperty("detail").GetString());
        Sender.VerifyAll();
    }

    [Fact]
    public async Task InvalidDesk_ReturnsValidationProblemWithBadRequestStatus()
    {
        // Arrange
        Sender.Setup(sender => sender.Send(It.IsAny<CreateDeskCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException([new ValidationFailure("Name", "Name is invalid.")]));

        // Act
        using var response = await Client.PostAsJsonAsync("/api/locations/11111111-1111-1111-1111-111111111111/desks",
            new { Name = "" }, TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Name is invalid.", problem.GetProperty("errors").GetProperty("Name")[0].GetString());
        Sender.VerifyAll();
    }

    [Fact]
    public async Task CreateDesk_UsesRouteLocationAndReturnsDetailsUrl()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var desk = new DeskDto(Guid.NewGuid(), "Window desk", null, locationId, true);
        Sender.Setup(sender => sender.Send(It.Is<CreateDeskCommand>(command =>
                command.LocationId == locationId && command.Name == desk.Name && command.Description == null),
            It.IsAny<CancellationToken>())).ReturnsAsync(desk);

        // Act
        using var response = await Client.PostAsJsonAsync($"/api/locations/{locationId}/desks",
            new { desk.Name, LocationId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"{Client.BaseAddress}api/locations/{locationId}/desks/{desk.Id}",
            response.Headers.Location?.ToString());
        Assert.Equal(desk, await response.Content.ReadFromJsonAsync<DeskDto>(TestContext.Current.CancellationToken));
        Sender.VerifyAll();
    }

    [Fact]
    public async Task GetDesks_MapsSearchFiltersToQuery()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        const string queryString = "?isAvailable=true&isBookable=false&startDate=2026-10-09&endDate=2026-10-10&page=2&pageSize=5";
        var expected = new GetDesksByLocationQuery(locationId,
            new DeskAvailabilityFilter(true, false),
            new DateRange(new DateOnly(2026, 10, 9), new DateOnly(2026, 10, 10)),
            new PaginationFilter(2, 5));
        Sender.Setup(sender => sender.Send(expected, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedDto<DeskDto>([], 1, 10, 0));

        // Act
        using var response = await Client.GetAsync($"/api/locations/{locationId}/desks{queryString}",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Sender.VerifyAll();
    }

    [Fact]
    public async Task ChangeReservedDesk_UsesRouteId()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var deskId = Guid.NewGuid();
        Sender.Setup(sender => sender.Send(new ChangeDeskCommand(reservationId, deskId),
            It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

        // Act
        using var response = await Client.PutAsJsonAsync($"/api/reservations/{reservationId}/change-desk",
            new { DeskId = deskId, Id = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Sender.VerifyAll();
    }

    [Theory]
    [InlineData("GET", "/api/locations/11111111-1111-1111-1111-111111111111/desks", null, HttpStatusCode.Unauthorized)]
    [InlineData("POST", "/api/locations", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("PUT", "/api/locations/11111111-1111-1111-1111-111111111111", null, HttpStatusCode.Unauthorized)]
    [InlineData("PUT", "/api/locations/11111111-1111-1111-1111-111111111111", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("POST", "/api/locations/11111111-1111-1111-1111-111111111111/desks", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("DELETE", "/api/locations/11111111-1111-1111-1111-111111111111/desks/22222222-2222-2222-2222-222222222222", PolicyNames.Employee, HttpStatusCode.Forbidden)]
    [InlineData("POST", "/api/reservations", null, HttpStatusCode.Unauthorized)]
    [InlineData("PUT", "/api/reservations/11111111-1111-1111-1111-111111111111/cancel", null, HttpStatusCode.Unauthorized)]
    [InlineData("GET", "/api/profile", null, HttpStatusCode.Unauthorized)]
    public async Task ProtectedEndpoints_EnforceAuthorization(string method, string path, string? role, HttpStatusCode status)
    {
        // Arrange
        Client.DefaultRequestHeaders.Remove("Test-Role");
        if (role is not null)
        {
            Client.DefaultRequestHeaders.Add("Test-Role", role);
        }

        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        // Act
        using var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await AssertProblemResponse(response, status, $"{method} {path}",
            status == HttpStatusCode.Unauthorized ? "15.5.2" : "15.5.4");
        if (status == HttpStatusCode.Unauthorized)
            Assert.Contains(response.Headers.WwwAuthenticate, header => string.Equals(header.Scheme, "Bearer", StringComparison.Ordinal));
        Assert.Empty(Sender.Invocations);
    }

    [Fact]
    public async Task UpdateLocation_UsesRouteIdAndReturnsUpdatedDetails()
    {
        // Arrange
        var id = Guid.NewGuid();
        var address = new AddressDto("Street", "10", "City", "00-001");
        var result = new LocationDto(id, "Updated", address);
        Sender.Setup(sender => sender.Send(new UpdateLocationCommand(id, result.Name, address), It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        using var response = await Client.PutAsJsonAsync($"/api/locations/{id}", new { result.Name, Address = address, Id = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(result, await response.Content.ReadFromJsonAsync<LocationDto>(TestContext.Current.CancellationToken));
        Sender.VerifyAll();
    }

    private static async Task<JsonElement> AssertProblemResponse(HttpResponseMessage response, HttpStatusCode status, string instance, string section)
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
        return problem;
    }
}
