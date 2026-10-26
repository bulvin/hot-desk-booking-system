using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Exceptions.Reservations;
using Domain.Reservations;
using Domain.Users;
using Unit = Mediator.Unit;

namespace Application.Reservations.Cancel;

public class CancelReservationHandler(
    IReservationRepository repository,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelReservationCommand, Unit>
{
    public async ValueTask<Unit> Handle(CancelReservationCommand command, CancellationToken cancellationToken)
    {
        var reservation = await repository.GetById(command.Id, cancellationToken)
            ?? throw new ReservationNotFoundException(command.Id);
        var user = userContext.GetCurrentUser();
        var isAdmin = user.Roles.Contains(nameof(UserRole.Administrator), StringComparer.Ordinal);
        if (!isAdmin && reservation.UserId != user.Id)
            throw new UnauthorizedReservationChangeException(command.Id, user.Id);

        if (reservation.Status == Status.Canceled)
            return Unit.Value;

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        var withinWindow = isAdmin ? today <= reservation.EndDate : today <= reservation.StartDate;
        if (reservation.Status != Status.Reserved || !withinWindow)
            throw new ReservationConflictException(command.Id);

        reservation.Status = Status.Canceled;
        await unitOfWork.SaveChanges(cancellationToken);
        return Unit.Value;
    }
}
