using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Reservations;
using Domain.Reservations;
using Unit = Mediator.Unit;
using Microsoft.AspNetCore.Http;

namespace Application.Reservations.ChangeDesk;

public class ChangeDeskHandler(
    IReservationRepository repository,
    IUnitOfWork unitOfWork,
    IDeskRepository deskRepository,
    IHttpContextAccessor httpContextAccessor)
    : ICommandHandler<ChangeDeskCommand, Unit>
{
    public async ValueTask<Unit> Handle(ChangeDeskCommand command, CancellationToken cancellationToken)
    {
        var reservation = await repository.GetById(command.Id, cancellationToken)
            ?? throw new ReservationNotFoundException(command.Id);

        if (reservation.DeskId == command.DeskId)
            throw new SameDeskChangeException(command.DeskId);

        if (reservation.UserId != httpContextAccessor.GetUserId())
            throw new UnauthorizedReservationChangeException(command.Id, httpContextAccessor.GetUserId());

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