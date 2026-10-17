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

        _handler = new GetDeskHandler(_userContextMock.Object, _deskRepositoryMock.Object);
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
}
