using Application.Interfaces.CQRS;

namespace Application.Locations;

public record CreateLocationCommand : ICommand<Guid>;

public class CreateLocationHandler : ICommandHandler<CreateLocationCommand, Guid>
{
    
    public CreateLocationHandler()
    {
        
    }
    public async Task<Guid> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        return await Task.FromResult(Guid.Empty);
    }
}