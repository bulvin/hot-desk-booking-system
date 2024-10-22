using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
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
                   ?? throw new ApplicationException("Desk not found");

        if (desk.LocationId != command.LocationId)
            throw new ApplicationException("Location not found");
        
        if (desk.IsAvailable == command.IsAvailable)
            throw new ApplicationException($"Desk is already {(command.IsAvailable ? "available" : "unavailable")}.");
        
        desk.IsAvailable = command.IsAvailable;
        _deskRepository.Update(desk);

        await _unitOfWork.SaveChanges(cancellationToken);
        return Unit.Value;
    }
}