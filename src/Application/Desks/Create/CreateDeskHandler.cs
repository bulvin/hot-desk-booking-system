using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Locations;
using Domain.Locations;

namespace Application.Desks.Create;

public record CreateDeskCommand(string Name, string? Description) : ICommand<DeskDto>
{
    public Guid LocationId { get; init; }
}
    
public class CreateDeskHandler : ICommandHandler<CreateDeskCommand, DeskDto>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IDeskRepository _deskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDeskHandler(IUnitOfWork unitOfWork, IDeskRepository deskRepository, ILocationRepository locationRepository)
    {
        _unitOfWork = unitOfWork;
        _deskRepository = deskRepository;
        _locationRepository = locationRepository;
    }

    public async Task<DeskDto> Handle(CreateDeskCommand command, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetById(command.LocationId, cancellationToken)
                       ?? throw new LocationNotFoundException(command.LocationId);

        var desk = new Desk
        {
            Name = command.Name, 
            Description = command.Description, 
            LocationId = command.LocationId
        };

        if (location.Desks.Any(d => d.Name == desk.Name))
            throw new DeskInLocationAlreadyExistsException(command.Name);
        
        _deskRepository.Add(desk);
       
        await _unitOfWork.SaveChanges(cancellationToken);

        return new DeskDto(
            desk.Id, 
            desk.Name, 
            desk.Description, 
            desk.LocationId, 
            desk.IsAvailable);
    }
}