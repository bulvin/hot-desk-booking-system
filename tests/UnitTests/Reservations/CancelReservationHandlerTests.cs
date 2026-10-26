using Application.Dtos;
using Application.Interfaces;
using Application.Reservations.Cancel;
using Domain;
using Domain.Exceptions.Reservations;
using Domain.Reservations;
using Moq;

namespace UnitTests.Reservations;

public class CancelReservationHandlerTests
{
    [Theory]
    [InlineData(false, 14, 23, 59, 59, Status.Reserved, true, 16)]
    [InlineData(false, 14, 23, 59, 59, Status.Reserved, true, 18)]
    [InlineData(false, 15, 0, 0, 0, Status.Reserved, true, 16)]
    [InlineData(false, 15, 0, 0, 0, Status.Reserved, true, 18)]
    [InlineData(false, 15, 23, 59, 59, Status.Reserved, true, 16)]
    [InlineData(false, 15, 23, 59, 59, Status.Reserved, true, 18)]
    [InlineData(false, 16, 0, 0, 0, Status.Reserved, true, 16)]
    [InlineData(false, 16, 0, 0, 0, Status.Reserved, true, 18)]
    [InlineData(false, 16, 23, 59, 59, Status.Reserved, true, 16)]
    [InlineData(false, 16, 23, 59, 59, Status.Reserved, true, 18)]
    [InlineData(false, 17, 0, 0, 0, Status.Reserved, false, 16)]
    [InlineData(false, 17, 0, 0, 0, Status.Reserved, false, 18)]
    [InlineData(true, 16, 0, 0, 0, Status.Reserved, true, 16)]
    [InlineData(true, 16, 0, 0, 0, Status.Reserved, true, 18)]
    [InlineData(true, 18, 23, 59, 59, Status.Reserved, false, 16)]
    [InlineData(true, 18, 23, 59, 59, Status.Reserved, true, 18)]
    [InlineData(true, 19, 0, 0, 0, Status.Reserved, false, 16)]
    [InlineData(true, 19, 0, 0, 0, Status.Reserved, false, 18)]
    [InlineData(true, 14, 0, 0, 0, Status.Completed, false, 16)]
    [InlineData(true, 14, 0, 0, 0, Status.Completed, false, 18)]
    [InlineData(false, 14, 0, 0, 0, Status.Completed, false, 16)]
    [InlineData(false, 14, 0, 0, 0, Status.Completed, false, 18)]
    [InlineData(false, 20, 0, 0, 0, Status.Canceled, true, 16)]
    [InlineData(false, 20, 0, 0, 0, Status.Canceled, true, 18)]
    [InlineData(true, 20, 0, 0, 0, Status.Canceled, true, 16)]
    [InlineData(true, 20, 0, 0, 0, Status.Canceled, true, 18)]
    public async Task Handle_EnforcesUtcWindowAndState(bool admin, int day, int hour, int minute, int second, Status status, bool allowed, int endDay)
    {
        var ownerId = Guid.NewGuid();
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            StartDate = new DateOnly(2026, 10, 16),
            EndDate = new DateOnly(2026, 10, endDay),
            Status = status
        };
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        repository.Setup(r => r.GetById(reservation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        var user = new Mock<IUserContext>(MockBehavior.Strict);
        user.Setup(u => u.GetCurrentUser()).Returns(new ProfileDto(admin ? Guid.NewGuid() : ownerId, "", "", "", admin ? ["Administrator"] : [], true));
        var clock = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2026, 10, day, hour, minute, second, DateTimeKind.Utc));
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var writes = allowed && status != Status.Canceled;
        if (writes)
            unit.Setup(u => u.SaveChanges(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new CancelReservationHandler(repository.Object, unit.Object, user.Object, clock.Object);

        if (allowed)
        {
            await handler.Handle(new CancelReservationCommand(reservation.Id), TestContext.Current.CancellationToken);
            Assert.Equal(Status.Canceled, reservation.Status);
        }
        else
        {
            await Assert.ThrowsAsync<ReservationConflictException>(() => handler.Handle(new CancelReservationCommand(reservation.Id), TestContext.Current.CancellationToken).AsTask());
            Assert.Equal(status, reservation.Status);
        }
        unit.Verify(u => u.SaveChanges(It.IsAny<CancellationToken>()), writes ? Times.Once : Times.Never);
        clock.VerifyGet(c => c.UtcNow, status == Status.Canceled ? Times.Never : Times.Once);
    }

    [Theory]
    [InlineData(Status.Reserved)]
    [InlineData(Status.Canceled)]
    [InlineData(Status.Completed)]
    public async Task Handle_NonOwnerCannotCancelOrInspectState(Status status)
    {
        var reservation = new Reservation { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = status };
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        repository.Setup(r => r.GetById(reservation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        var user = new Mock<IUserContext>(MockBehavior.Strict);
        user.Setup(u => u.GetCurrentUser()).Returns(new ProfileDto(Guid.NewGuid(), "", "", "", [], true));
        var handler = new CancelReservationHandler(repository.Object, new Mock<IUnitOfWork>(MockBehavior.Strict).Object,
            user.Object, new Mock<IDateTimeProvider>(MockBehavior.Strict).Object);

        await Assert.ThrowsAsync<UnauthorizedReservationChangeException>(() => handler.Handle(new CancelReservationCommand(reservation.Id), TestContext.Current.CancellationToken).AsTask());
        Assert.Equal(status, reservation.Status);
    }

    [Fact]
    public async Task Handle_UnknownReservationIsNotFound()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        repository.Setup(r => r.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((Reservation?)null);
        var handler = new CancelReservationHandler(repository.Object, new Mock<IUnitOfWork>(MockBehavior.Strict).Object,
            new Mock<IUserContext>(MockBehavior.Strict).Object, new Mock<IDateTimeProvider>(MockBehavior.Strict).Object);
        await Assert.ThrowsAsync<ReservationNotFoundException>(() => handler.Handle(new CancelReservationCommand(id), TestContext.Current.CancellationToken).AsTask());
    }
}
