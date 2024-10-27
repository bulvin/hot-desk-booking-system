using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions;
using Domain.Exceptions.Desks;
using Domain.Locations;
using Domain.Reservations;
using MediatR;

namespace Application.Desks.Delete;

public record DeleteDeskCommand(Guid LocationId, Guid DeskId) : ICommand<Unit>;

public class DeleteDeskHandler : ICommandHandler<DeleteDeskCommand, Unit>
{
    private readonly IDeskRepository _deskRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDeskHandler(IUnitOfWork unitOfWork, IDeskRepository repository, IDeskRepository deskRepository, IReservationRepository reservationRepository)
    {
        _unitOfWork = unitOfWork;
        _deskRepository = deskRepository;
        _reservationRepository = reservationRepository;
   
    }

    public async Task<Unit> Handle(DeleteDeskCommand command, CancellationToken cancellationToken)
    {
        var desk = await _deskRepository.GetByIdAndLocation(command.DeskId, command.LocationId, cancellationToken)
                       ?? throw new DeskNotFoundException(command.DeskId);

        var hasReservation = await _reservationRepository.HasActiveReservationForDesk(desk.Id, cancellationToken);
        if (hasReservation)
            throw new DeskHasActiveReservationException(command.DeskId);
        
        _deskRepository.Delete(desk);
        await _unitOfWork.SaveChanges(cancellationToken);
        return Unit.Value;
    }
}