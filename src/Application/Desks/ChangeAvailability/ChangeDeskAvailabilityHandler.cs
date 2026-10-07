using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Locations;
using MediatR;

namespace Application.Desks.ChangeAvailability;

public class ChangeDeskAvailabilityHandler(IUnitOfWork unitOfWork, IDeskRepository deskRepository)
    : ICommandHandler<ChangeDeskAvailabilityCommand, Unit>
{
    public async Task<Unit> Handle(ChangeDeskAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var desk = await deskRepository.GetById(command.Id, cancellationToken)
                   ?? throw new DeskNotFoundException(command.Id);

        if (desk.LocationId != command.LocationId)
            throw new LocationNotFoundException(command.LocationId);

        if (desk.IsAvailable == command.IsAvailable)
            throw new DeskAvailabilityException(command.IsAvailable);

        desk.IsAvailable = command.IsAvailable;
        deskRepository.Update(desk);

        await unitOfWork.SaveChanges(cancellationToken);
        return Unit.Value;
    }
}