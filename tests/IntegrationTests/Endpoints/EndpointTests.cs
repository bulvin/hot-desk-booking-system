using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.Desks.ChangeAvailability;
using Application.Desks.Create;
using Application.Desks.GetDetails;
using Application.Desks.GetPagedByLocation;
using Application.Dtos;
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

    [Theory]
    [InlineData("Administrator")]
    [InlineData("Employee")]
    public async Task Cancel_DispatchesRouteIdAndReturnsNoContent(string role)
    {
        // Arrange
        var id = Guid.NewGuid();
        Client.DefaultRequestHeaders.Remove("Test-Role");
        Client.DefaultRequestHeaders.Add("Test-Role", role);
        Sender.Setup(s => s.Send(new CancelReservationCommand(id), It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

        // Act
        using var response = await Client.PutAsync($"/api/reservations/{id}/cancel", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Sender.Verify(s => s.Send(new CancelReservationCommand(id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_AnonymousIsUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Remove("Test-Role");
        var path = $"/api/reservations/{Guid.NewGuid()}/cancel";

        // Act
        using var response = await Client.PutAsync(path, null, TestContext.Current.CancellationToken);

        // Assert
        await AssertProblemResponse(response, HttpStatusCode.Unauthorized, $"PUT {path}", "15.5.2");
        Assert.Empty(Sender.Invocations);
    }

    [Theory]
    [InlineData(403, "15.5.4")]
    [InlineData(404, "15.5.5")]
    [InlineData(409, "15.5.10")]
    public async Task Cancel_ReturnsDocumentedProblems(int status, string section)
    {
        // Arrange
        var id = Guid.NewGuid();
        Exception exception = status switch
        {
            403 => new UnauthorizedReservationChangeException(id, Guid.NewGuid()),
            404 => new ReservationNotFoundException(id),
            _ => new ReservationConflictException(id)
        };
        Sender.Setup(s => s.Send(new CancelReservationCommand(id), It.IsAny<CancellationToken>())).ThrowsAsync(exception);
        var path = $"/api/reservations/{id}/cancel";

        // Act
        using var response = await Client.PutAsync(path, null, TestContext.Current.CancellationToken);

        // Assert
        await AssertProblemResponse(response, (HttpStatusCode)status, $"PUT {path}", section);
    }

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

    [Theory]
    [InlineData("GET", "/api/missing-route", HttpStatusCode.NotFound, "15.5.5")]
    [InlineData("GET", "/api/locations/11111111-1111-1111-1111-111111111111/desks?page=invalid", HttpStatusCode.BadRequest, "15.5.1")]
    public async Task EmptyErrors_ReturnProblemDetails(string method, string path, HttpStatusCode status, string section)
    {
        // Arrange
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Accept.ParseAdd("application/problem+json");

        // Act
        using var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await AssertProblemResponse(response, status, $"{method} {path.Split('?')[0]}", section);
        Assert.Empty(Sender.Invocations);
    }

    [Theory]
    [InlineData("POST", "/api/locations/11111111-1111-1111-1111-111111111111/desks", "application/json", "{", HttpStatusCode.BadRequest, "15.5.1")]
    [InlineData("POST", "/api/locations/11111111-1111-1111-1111-111111111111/desks", "text/plain", "text", HttpStatusCode.UnsupportedMediaType, "15.5.16")]
    [InlineData("PUT", "/api/locations/11111111-1111-1111-1111-111111111111", "application/json", "{", HttpStatusCode.BadRequest, "15.5.1")]
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
        // Arrange

        // Act
        var document = _host.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var operation = document.Paths[path].Operations!
            .Single(pair => string.Equals(pair.Key.ToString(), method, StringComparison.OrdinalIgnoreCase)).Value;

        // Assert
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

    [Theory]
    [InlineData("")]
    [InlineData("?isAvailable=true&isBookable=false&startDate=2026-10-09&endDate=2026-10-10&page=2&pageSize=5")]
    public async Task GetDesks_BindsOptionalQueryParameters(string queryString)
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var hasFilters = queryString.Length > 0;
        var expected = new GetDesksByLocationQuery(locationId,
            new DeskAvailabilityFilter(hasFilters ? true : null, hasFilters ? false : null),
            new DateRange(hasFilters ? new DateOnly(2026, 10, 9) : null, hasFilters ? new DateOnly(2026, 10, 10) : null),
            new PaginationFilter(hasFilters ? 2 : null, hasFilters ? 5 : null));
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
    public async Task ChangeAvailability_BindsQueryAndReturnsNoContent()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var deskId = Guid.NewGuid();
        Sender.Setup(sender => sender.Send(new ChangeDeskAvailabilityCommand(deskId, locationId, false),
            It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

        // Act
        using var response = await Client.PutAsync($"/api/locations/{locationId}/desks/{deskId}?isAvailable=false",
            null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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

    [Theory]
    [InlineData(400, "15.5.1")]
    [InlineData(404, "15.5.5")]
    [InlineData(409, "15.5.10")]
    public async Task UpdateLocation_FailuresReturnProblemDetails(int status, string section)
    {
        // Arrange
        var id = Guid.NewGuid();
        Exception exception = status switch
        {
            400 => new ValidationException([new ValidationFailure("Name", "Name is required.")]),
            404 => new LocationNotFoundException(id),
            _ => new LocationAlreadyExistsException("Duplicate", new Address("Street", "1", "City", "Code"))
        };
        Sender.Setup(sender => sender.Send(It.IsAny<UpdateLocationCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        // Act
        using var response = await Client.PutAsJsonAsync($"/api/locations/{id}", new { Name = "Name", Address = new AddressDto("Street", "1", "City", "Code") }, TestContext.Current.CancellationToken);

        // Assert
        await AssertProblemResponse(response, (HttpStatusCode)status, $"PUT /api/locations/{id}", section);
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
