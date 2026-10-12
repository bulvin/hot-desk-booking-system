using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Reservations;
using Domain.Reservations;
using Unit = Mediator.Unit;

namespace Application.Reservations.ChangeDesk;

public class ChangeDeskHandler(
    IReservationRepository repository,
    IUnitOfWork unitOfWork,
    IDeskRepository deskRepository,
    IUserContext userContext)
    : ICommandHandler<ChangeDeskCommand, Unit>
{
    public async ValueTask<Unit> Handle(ChangeDeskCommand command, CancellationToken cancellationToken)
    {
        var reservation = await repository.GetById(command.Id, cancellationToken)
            ?? throw new ReservationNotFoundException(command.Id);

        if (reservation.DeskId == command.DeskId)
            throw new SameDeskChangeException(command.DeskId);

        var userId = userContext.GetCurrentUser().Id;
        if (reservation.UserId != userId)
            throw new UnauthorizedReservationChangeException(command.Id, userId);

        if (!await deskRepository.Exists(command.DeskId, cancellationToken))
            throw new DeskNotFoundException(command.DeskId);

        if (reservation.StartDate < DateOnly.FromDateTime(DateTime.Now.AddHours(24)))
        {
            throw new TooLateForReservationChangeException(command.Id, reservation.StartDate);
        }

        reservation.DeskId = command.DeskId;
        await unitOfWork.SaveChanges(cancellationToken);

        return Unit.Value;
    }
}