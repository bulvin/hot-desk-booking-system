using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Reservations;
using MediatR;

namespace Application.Reservations.ChangeDesk;

public record ChangeDeskCommand(Guid Id, Guid DeskId) : ICommand<Unit>;
    
public class ChangeDeskHandler : ICommandHandler<ChangeDeskCommand, Unit>
{
    private readonly IReservationRepository _repository;
    private readonly IDeskRepository _deskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeDeskHandler(IReservationRepository repository, IUnitOfWork unitOfWork, IDeskRepository deskRepository)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _deskRepository = deskRepository;
    }

    public async Task<Unit> Handle(ChangeDeskCommand command, CancellationToken cancellationToken)
    {
        var reservation = await _repository.GetById(command.Id, cancellationToken)
            ?? throw new ApplicationException("Reservation not found");

        if (reservation.DeskId == command.DeskId)
            throw new ApplicationException("You cannot change the same desk");
        
        if (!await _deskRepository.Exists(command.DeskId, cancellationToken))
            throw new ApplicationException("Desk not found");
        
        if (reservation.StartDate < DateOnly.FromDateTime(DateTime.Now.AddHours(24)))
        {
            throw new ApplicationException("You can only change the desk at least 24 hours before the reservation.");
        }

        reservation.DeskId = command.DeskId;
        await _unitOfWork.SaveChanges(cancellationToken);
        
        return Unit.Value;
    }
}