using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions.Desks;
using Domain.Reservations;
using Unit = Mediator.Unit;

namespace Application.Desks.Delete;

public class DeleteDeskHandler(
    IUnitOfWork unitOfWork,
    IDeskRepository deskRepository,
    IReservationRepository reservationRepository)
    : ICommandHandler<DeleteDeskCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteDeskCommand command, CancellationToken cancellationToken)
    {
        var desk = await deskRepository.GetByIdAndLocation(command.DeskId, command.LocationId, cancellationToken)
                       ?? throw new DeskNotFoundException(command.DeskId);

        var hasReservation = await reservationRepository.HasActiveReservationForDesk(desk.Id, cancellationToken);
        if (hasReservation)
            throw new DeskHasActiveReservationException(command.DeskId);

        deskRepository.Delete(desk);
        await unitOfWork.SaveChanges(cancellationToken);
        return Unit.Value;
    }
}