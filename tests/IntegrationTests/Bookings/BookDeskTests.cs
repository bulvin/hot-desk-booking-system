using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Dtos;
using Domain.Reservations;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Bookings;

[Trait("Category", "Integration")]
[Collection("Application host")]
public sealed class BookDeskTests(BookingApiFixture fixture) : IClassFixture<BookingApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task BookDesk_VerifiedKeycloakEmployee_PersistsBookingAndUser()
    {
        // Arrange
        var deskId = await fixture.CreateDesk();
        using var client = fixture.CreateClient();
        var start = BookingApiFixture.Today.AddDays(1);
        var end = start.AddDays(6);

        // Act
        using var response = await client.PostAsJsonAsync("/api/reservations",
            new { DeskId = deskId, StartDate = start, EndDate = end }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal($"/api/reservations/{result.Id}", response.Headers.Location?.OriginalString);
        await using var db = fixture.CreateDbContext();
        var booking = await db.Reservations.Include(reservation => reservation.User)
            .SingleAsync(reservation => reservation.Id == result.Id, TestContext.Current.CancellationToken);
        Assert.Equal(deskId, booking.DeskId);
        Assert.Equal(BookingApiFixture.EmployeeId, booking.UserId);
        Assert.Equal("employee@example.test", booking.User.Email);
        Assert.Equal(start, booking.StartDate);
        Assert.Equal(end, booking.EndDate);
        Assert.Equal(Status.Reserved, booking.Status);
        Assert.Equal(new ReservationDto(booking.Id, start, end, Status.Reserved), result);
    }

    [Fact]
    public async Task BookDesk_OverlappingFinalDay_ReturnsConflictWithoutSecondBooking()
    {
        // Arrange
        var deskId = await fixture.CreateDesk();
        using var client = fixture.CreateClient();
        var start = BookingApiFixture.Today.AddDays(1);
        var end = start.AddDays(2);
        using var first = await client.PostAsJsonAsync("/api/reservations",
            new { DeskId = deskId, StartDate = start, EndDate = end }, TestContext.Current.CancellationToken);
        first.EnsureSuccessStatusCode();

        // Act
        using var response = await client.PostAsJsonAsync("/api/reservations",
            new { DeskId = deskId, StartDate = end, EndDate = end.AddDays(1) }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await using var db = fixture.CreateDbContext();
        var booking = Assert.Single(await db.Reservations.Where(reservation => reservation.DeskId == deskId)
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(start, booking.StartDate);
        Assert.Equal(end, booking.EndDate);
    }

    [Fact]
    public async Task BookDesk_DisabledDesk_ReturnsBadRequestWithoutPersistingBooking()
    {
        // Arrange
        var deskId = await fixture.CreateDesk(available: false);
        using var client = fixture.CreateClient();
        var date = BookingApiFixture.Today.AddDays(1);

        // Act
        using var response = await client.PostAsJsonAsync("/api/reservations",
            new { DeskId = deskId, StartDate = date, EndDate = date }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Reservations.AnyAsync(reservation => reservation.DeskId == deskId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BookDesk_AfterOwnerCancels_ReusesDatesAndRetainsHistory()
    {
        // Arrange
        var deskId = await fixture.CreateDesk();
        using var client = fixture.CreateClient();
        var date = BookingApiFixture.Today.AddDays(1);
        var request = new { DeskId = deskId, StartDate = date, EndDate = date };
        using var first = await client.PostAsJsonAsync("/api/reservations", request, TestContext.Current.CancellationToken);
        first.EnsureSuccessStatusCode();
        var original = await first.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(original);
        using var cancellation = await client.PutAsync($"/api/reservations/{original.Id}/cancel", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, cancellation.StatusCode);

        // Act
        using var response = await client.PostAsJsonAsync("/api/reservations", request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var replacement = await response.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(replacement);
        Assert.NotEqual(original.Id, replacement.Id);
        await using var db = fixture.CreateDbContext();
        var bookings = await db.Reservations.Where(reservation => reservation.DeskId == deskId)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, bookings.Count);
        Assert.Contains(bookings, booking => booking.Id == original.Id && booking.Status == Status.Canceled);
        Assert.Contains(bookings, booking => booking.Id == replacement.Id && booking.Status == Status.Reserved);
    }

    [Fact]
    public async Task BookDesk_WithoutKeycloakToken_ReturnsUnauthorizedWithoutPersistingBooking()
    {
        // Arrange
        var deskId = await fixture.CreateDesk();
        using var client = fixture.CreateClient(authenticated: false);
        var date = BookingApiFixture.Today.AddDays(1);

        // Act
        using var response = await client.PostAsJsonAsync("/api/reservations",
            new { DeskId = deskId, StartDate = date, EndDate = date }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Reservations.AnyAsync(reservation => reservation.DeskId == deskId, TestContext.Current.CancellationToken));
    }
}
