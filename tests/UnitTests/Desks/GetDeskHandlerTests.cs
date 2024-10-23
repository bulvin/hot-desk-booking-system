using System.Security.Claims;
using Application.Desks.Get;
using Application.Interfaces;
using Domain.Desks;
using Domain.Reservations;
using Domain.Users;
using Infrastructure.Time;
using Microsoft.AspNetCore.Http;
using Moq;

namespace UnitTests.Desks;


public class GetDeskHandlerTests
{
    private readonly Mock<IDeskRepository> _deskRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ClaimsPrincipal> _userMock;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly GetDeskHandler _handler;

    public GetDeskHandlerTests()
    {
        _deskRepositoryMock = new Mock<IDeskRepository>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextMock = new Mock<HttpContext>();
        _userMock = new Mock<ClaimsPrincipal>();
        _dateTimeProvider = new DateTimeProvider { UtcNow = DateTime.UtcNow };
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        
        _handler = new GetDeskHandler(_httpContextAccessorMock.Object, _deskRepositoryMock.Object);
    }

    private void SetupUserRole(bool isAdmin)
    {
        _userMock
            .Setup(x => x.IsInRole(UserRole.Administrator.ToString()))
            .Returns(isAdmin);
    }
    
    [Fact]
    public async Task Handle_WhenDeskNotFound_ThrowsApplicationException()
    {
   
        var query = new GetDeskDetailsQuery(Guid.NewGuid(), Guid.NewGuid());
        
        _deskRepositoryMock
            .Setup(x => x.GetById(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Desk)null);

    
        var exception = await Assert.ThrowsAsync<ApplicationException>(
            () => _handler.Handle(query, CancellationToken.None));
        
        Assert.Equal("desk not found", exception.Message);
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

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var tomorrow = today.AddDays(1);

        SetupUserRole(isAdmin);

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = userId,
            User = new User 
            { 
                Id = userId,
                FirstName = "John",
                LastName = "Doe"
            },
            StartDate = today,
            EndDate = tomorrow,
            Status = Status.Active
        };

        var desk = new Desk
        {
            Id = deskId,
            LocationId = locationId,
            Name = "Test Desk",
            Description = "Test Description",
            IsAvailable = true,
            Reservations = new List<Reservation> { reservation }
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
        Assert.Equal(Status.Active, result.Reservation.Status);

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