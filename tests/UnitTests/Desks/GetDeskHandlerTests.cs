using Application.Desks.GetDetails;
using Application.Dtos;
using Application.Interfaces;
using Domain.Desks;
using Domain.Exceptions.Desks;
using Domain.Reservations;
using Domain.Users;
using Moq;

namespace UnitTests.Desks;

public class GetDeskHandlerTests
{
    private readonly Mock<IDeskRepository> _deskRepositoryMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly GetDeskHandler _handler;

    public GetDeskHandlerTests()
    {
        _deskRepositoryMock = new Mock<IDeskRepository>();
        _userContextMock = new Mock<IUserContext>();

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc));
        _handler = new GetDeskHandler(_userContextMock.Object, _deskRepositoryMock.Object, clock.Object);
    }

    private void SetupUserRole(bool isAdmin)
    {
        _userContextMock
            .Setup(x => x.GetCurrentUser())
            .Returns(new ProfileDto(Guid.NewGuid(), "actor@example.com", "Current", "User",
                isAdmin ? [UserRole.Administrator.ToString()] : [UserRole.Employee.ToString()], true));
    }

    [Fact]
    public async Task Handle_WhenDeskNotFound_ThrowsDeskNotFoundException()
    {

        var query = new GetDeskDetailsQuery(Guid.NewGuid(), Guid.NewGuid());

        _deskRepositoryMock
            .Setup(x => x.GetById(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Desk?)null);


        var exception = await Assert.ThrowsAsync<DeskNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None).AsTask());

        Assert.Equal(query.Id, exception.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WithActiveReservation_ReturnsCorrectDeskDetails(bool isAdmin)
    {
        var deskId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var query = new GetDeskDetailsQuery(deskId, locationId);

        var today = new DateOnly(2026, 10, 15);
        var tomorrow = today.AddDays(1);

        SetupUserRole(isAdmin);

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = userId,
            User = new User { Id = userId, FirstName = "John", LastName = "Doe" },
            StartDate = today,
            EndDate = tomorrow,
            Status = Status.Reserved
        };

        var desk = new Desk
        {
            Id = deskId,
            LocationId = locationId,
            Name = "Test Desk",
            Description = "Test Description",
            IsAvailable = true,
            Reservations = { reservation }
        };

        _deskRepositoryMock
            .Setup(x => x.GetById(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(desk);


        var result = await _handler.Handle(query, CancellationToken.None);


        Assert.NotNull(result);
        Assert.Equal(deskId, result.Id);
        Assert.Equal(locationId, result.LocationId);
        Assert.Equal("Test Desk", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.True(result.IsAvailable);

        Assert.NotNull(result.Reservation);
        Assert.Equal(reservationId, result.Reservation.Id);
        Assert.Equal(today, result.Reservation.StartDate);
        Assert.Equal(tomorrow, result.Reservation.EndDate);
        Assert.Equal(Status.Reserved, result.Reservation.Status);

        if (isAdmin)
        {
            Assert.NotNull(result.Reservation.User);
            Assert.Equal(userId, result.Reservation.User.Id);
            Assert.Equal("John Doe", result.Reservation.User.Name);
        }
        else
        {
            Assert.Null(result.Reservation.User);
        }
    }
    [Theory]
    [InlineData(-2, 0, Status.Reserved, true)]
    [InlineData(0, 0, Status.Reserved, true)]
    [InlineData(1, 2, Status.Reserved, true)]
    [InlineData(-2, -1, Status.Reserved, false)]
    [InlineData(0, 1, Status.Canceled, false)]
    [InlineData(0, 1, Status.Completed, false)]
    public async Task Handle_WithReservationDatesAndStatus_SelectsOnlyCurrentOrUpcomingReservedBooking(
        int startOffset, int endOffset, Status status, bool expected)
    {
        // Arrange
        SetupUserRole(true);
        var today = new DateOnly(2026, 10, 15);
        var desk = new Desk { Id = Guid.NewGuid(), LocationId = Guid.NewGuid() };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            StartDate = today.AddDays(startOffset),
            EndDate = today.AddDays(endOffset),
            Status = status,
            User = new User()
        };
        desk.Reservations.Add(reservation);
        _deskRepositoryMock.Setup(x => x.GetById(desk.Id, It.IsAny<CancellationToken>())).ReturnsAsync(desk);

        // Act
        var result = await _handler.Handle(new GetDeskDetailsQuery(desk.Id, desk.LocationId), CancellationToken.None);

        // Assert
        Assert.Equal(expected ? reservation.Id : (Guid?)null, result.Reservation?.Id);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task Handle_WithUnorderedReservations_ReturnsEarliestUnexpiredBooking(int startOffset)
    {
        // Arrange
        SetupUserRole(true);
        var today = new DateOnly(2026, 10, 15);
        var desk = new Desk { Id = Guid.NewGuid(), LocationId = Guid.NewGuid() };
        var later = new Reservation
        {
            Id = Guid.NewGuid(),
            StartDate = today.AddDays(3),
            EndDate = today.AddDays(3),
            User = new User()
        };
        var earlier = new Reservation
        {
            Id = Guid.NewGuid(),
            StartDate = today.AddDays(startOffset),
            EndDate = today.AddDays(1),
            User = new User()
        };
        desk.Reservations.Add(later);
        desk.Reservations.Add(earlier);
        _deskRepositoryMock.Setup(x => x.GetById(desk.Id, It.IsAny<CancellationToken>())).ReturnsAsync(desk);

        // Act
        var result = await _handler.Handle(new GetDeskDetailsQuery(desk.Id, desk.LocationId), CancellationToken.None);

        // Assert
        Assert.Equal(earlier.Id, result.Reservation?.Id);
    }
}
