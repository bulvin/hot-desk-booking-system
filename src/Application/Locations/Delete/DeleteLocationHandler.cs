using Application.Interfaces.CQRS;
using Domain;
using Domain.Exceptions.Locations;
using Domain.Locations;
using Unit = Mediator.Unit;

namespace Application.Locations.Delete;

public class DeleteLocationHandler(ILocationRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteLocationCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await repository.GetById(command.Id, cancellationToken)
                       ?? throw new LocationNotFoundException(command.Id);

        if (location.Desks.Count != 0)
        {
            throw new LocationHasDesksException(location.Id);
        }

        repository.Delete(location);
        await unitOfWork.SaveChanges(cancellationToken);

        return Unit.Value;
    }
}