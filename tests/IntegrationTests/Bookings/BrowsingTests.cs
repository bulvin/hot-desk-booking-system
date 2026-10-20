using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Dtos;
using Domain.Reservations;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Bookings;

[Trait("Category", "Integration")]
public sealed class BrowsingTests(BookingApiFixture fixture) : IClassFixture<BookingApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Theory]
    [InlineData("/api/locations")]
    [InlineData("/api/reservations/mine")]
    public async Task Browse_Anonymous_ReturnsUnauthorized(string path)
    {
        // Arrange
        using var client = fixture.CreateClient(authenticated: false);

        // Act
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/locations", "page=0")]
    [InlineData("/api/locations", "pageSize=31")]
    [InlineData("/api/reservations/mine", "page=-1")]
    [InlineData("/api/reservations/mine", "pageSize=0")]
    public async Task Browse_InvalidPagination_ReturnsValidationProblem(string path, string query)
    {
        // Arrange
        using var client = fixture.CreateClient();

        // Act
        using var response = await client.GetAsync($"{path}?{query}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(problem.RootElement.GetProperty("errors").EnumerateObject().Any());
    }

    [Fact]
    public async Task Locations_Employee_CanBrowseWithStablePagination()
    {
        // Arrange
        await fixture.CreateDesk();
        await fixture.CreateDesk();
        using var client = fixture.CreateClient();

        // Act
        var first = await client.GetFromJsonAsync<PagedDto<LocationDto>>("/api/locations?pageSize=1", TestContext.Current.CancellationToken);
        var second = await client.GetFromJsonAsync<PagedDto<LocationDto>>("/api/locations?page=2&pageSize=1", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Single(first.Items);
        Assert.Single(second.Items);
        Assert.NotEqual(first.Items[0].Id, second.Items[0].Id);
        Assert.True(first.HasNextPage);
        Assert.True(second.HasPreviousPage);
        Assert.False(string.IsNullOrEmpty(first.Items[0].Address.City));
    }

    [Fact]
    public async Task Mine_ReturnsOrderedHistoryWithDeskAndOffice_ExcludesOtherOwners()
    {
        // Arrange
        var deskId = await fixture.CreateDesk();
        using var client = fixture.CreateClient();
        using var booking = await client.PostAsJsonAsync("/api/reservations", new
        {
            DeskId = deskId,
            StartDate = BookingApiFixture.Today,
            EndDate = BookingApiFixture.Today
        }, TestContext.Current.CancellationToken);
        booking.EnsureSuccessStatusCode();
        var own = await booking.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(own);
        var other = new Reservation
        {
            DeskId = deskId,
            StartDate = BookingApiFixture.Today.AddDays(1),
            EndDate = BookingApiFixture.Today.AddDays(1),
            User = new User { Email = "private@example.test", FirstName = "Private", LastName = "Owner" }
        };
        var history = new Reservation
        {
            DeskId = deskId,
            UserId = BookingApiFixture.EmployeeId,
            StartDate = BookingApiFixture.Today.AddDays(-1),
            EndDate = BookingApiFixture.Today.AddDays(-1),
            Status = Status.Canceled
        };
        await using (var db = fixture.CreateDbContext())
        {
            db.Reservations.AddRange(other, history);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var result = await client.GetFromJsonAsync<PagedDto<MyReservationDto>>("/api/reservations/mine?pageSize=30", JsonOptions, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Items, x => x.Id == own.Id && x.DeskId == deskId && string.Equals(x.DeskName, "Integration desk", StringComparison.Ordinal) && string.Equals(x.LocationName, "Integration office", StringComparison.Ordinal));
        Assert.Contains(result.Items, x => x.Id == history.Id && x.Status == Status.Canceled);
        Assert.DoesNotContain(result.Items, x => x.Id == other.Id);
        Assert.Equal(result.Items.OrderByDescending(x => x.StartDate).ThenBy(x => x.Id), result.Items);
        await using var verify = fixture.CreateDbContext();
        var ids = result.Items.Select(x => x.Id).ToList();
        Assert.All(await verify.Reservations.Where(x => ids.Contains(x.Id)).ToListAsync(TestContext.Current.CancellationToken),
            x => Assert.Equal(BookingApiFixture.EmployeeId, x.UserId));
    }
}
