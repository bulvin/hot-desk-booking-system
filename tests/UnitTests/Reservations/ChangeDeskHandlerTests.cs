using Application.Dtos;
using Application.Interfaces;
using Application.Reservations.ChangeDesk;
using Domain;
using Domain.Desks;
using Domain.Exceptions.Reservations;
using Domain.Reservations;
using Moq;

namespace UnitTests.Reservations;

public class ChangeDeskHandlerTests
{
    [Theory]
    [InlineData(15, 23, 59, 59, true)]
    [InlineData(16, 0, 0, 0, false)]
    [InlineData(17, 0, 0, 0, false)]
    public async Task Handle_EnforcesUtcStartDayCutoff(int day, int hour, int minute, int second, bool allowed)
    {
        var clock = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        clock.SetupGet(provider => provider.UtcNow).Returns(new DateTime(2026, 10, day, hour, minute, second, DateTimeKind.Utc));
        var userId = Guid.NewGuid();
        var originalDeskId = Guid.NewGuid();
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            DeskId = originalDeskId,
            UserId = userId,
            StartDate = new DateOnly(2026, 10, 16),
            EndDate = new DateOnly(2026, 10, 16)
        };
        var command = new ChangeDeskCommand(reservation.Id, Guid.NewGuid());
        var reservations = new Mock<IReservationRepository>(MockBehavior.Strict);
        reservations.Setup(repo => repo.GetById(command.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        var desks = new Mock<IDeskRepository>(MockBehavior.Strict);
        desks.Setup(repo => repo.Exists(command.DeskId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var user = new Mock<IUserContext>(MockBehavior.Strict);
        user.Setup(context => context.GetCurrentUser()).Returns(new ProfileDto(userId, "owner@example.com", "Desk", "Owner", [], true));
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        if (allowed)
            unit.Setup(work => work.SaveChanges(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new ChangeDeskHandler(reservations.Object, unit.Object, desks.Object, user.Object, clock.Object);

        if (allowed)
        {
            await handler.Handle(command, TestContext.Current.CancellationToken);
            Assert.Equal(command.DeskId, reservation.DeskId);
        }
        else
        {
            await Assert.ThrowsAsync<TooLateForReservationChangeException>(() => handler.Handle(command, TestContext.Current.CancellationToken).AsTask());
            Assert.Equal(originalDeskId, reservation.DeskId);
        }

        unit.Verify(work => work.SaveChanges(It.IsAny<CancellationToken>()), allowed ? Times.Once : Times.Never);
        clock.VerifyGet(provider => provider.UtcNow, Times.Once);
    }
    [Theory]
    [InlineData(Status.Canceled)]
    [InlineData(Status.Completed)]
    public async Task Handle_InactiveReservationCannotChangeDesk(Status status)
    {
        var owner = Guid.NewGuid();
        var reservation = new Reservation { Id = Guid.NewGuid(), UserId = owner, Status = status };
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        repository.Setup(r => r.GetById(reservation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        var user = new Mock<IUserContext>(MockBehavior.Strict);
        user.Setup(u => u.GetCurrentUser()).Returns(new ProfileDto(owner, "", "", "", [], true));
        var clock = new Mock<IDateTimeProvider>();
        var handler = new ChangeDeskHandler(repository.Object, new Mock<IUnitOfWork>(MockBehavior.Strict).Object,
            new Mock<IDeskRepository>(MockBehavior.Strict).Object, user.Object, clock.Object);
        await Assert.ThrowsAsync<ReservationConflictException>(() => handler.Handle(new ChangeDeskCommand(reservation.Id, Guid.NewGuid()), TestContext.Current.CancellationToken).AsTask());
        Assert.Equal(status, reservation.Status);
    }
}
