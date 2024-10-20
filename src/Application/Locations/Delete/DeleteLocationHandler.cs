using Application.Interfaces.CQRS;
using Domain;
using Domain.Locations;
using MediatR;

namespace Application.Locations.Delete;

public record DeleteLocationCommand(Guid Id) : ICommand<Unit>;

public class DeleteLocationHandler : ICommandHandler<DeleteLocationCommand, Unit>
{
    private readonly ILocationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLocationHandler(ILocationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await _repository.GetById(command.Id, cancellationToken) 
                       ?? throw new ApplicationException("Location Not Found");
        
        if (location.Desks.Count != 0)
        {
            throw new ApplicationException("Cannot delete location with existing desks");
        }

        _repository.Delete(location);
        await _unitOfWork.SaveChanges(cancellationToken);

        return Unit.Value;
    }
}