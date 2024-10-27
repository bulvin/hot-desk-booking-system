using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Locations;
using MediatR;

namespace Application.Desks.ChangeAvailability;

public record ChangeDeskAvailabilityCommand(Guid Id, Guid LocationId, bool IsAvailable) : ICommand<Unit>;

public class ChangeDeskAvailabilityHandler : ICommandHandler<ChangeDeskAvailabilityCommand, Unit>
{
    private readonly IDeskRepository _deskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeDeskAvailabilityHandler(IUnitOfWork unitOfWork, IDeskRepository deskRepository)
    {
        _unitOfWork = unitOfWork;
        _deskRepository = deskRepository;
    }

    public async Task<Unit> Handle(ChangeDeskAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var desk = await _deskRepository.GetById(command.Id, cancellationToken)
                   ?? throw new DeskNotFoundException(command.Id);

        if (desk.LocationId != command.LocationId)
            throw new LocationNotFoundException(command.LocationId);
        
        if (desk.IsAvailable == command.IsAvailable)
            throw new DeskAvailabilityException(command.IsAvailable);
        
        desk.IsAvailable = command.IsAvailable;
        _deskRepository.Update(desk);

        await _unitOfWork.SaveChanges(cancellationToken);
        return Unit.Value;
    }
}