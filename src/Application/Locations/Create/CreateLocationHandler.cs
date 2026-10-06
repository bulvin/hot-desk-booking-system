using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Exceptions;
using Domain.Exceptions.Locations;
using Domain.Locations;

namespace Application.Locations.Create;

public record CreateLocationCommand(string Name, AddressDto Address) : ICommand<LocationDto>;

public class CreateLocationHandler : ICommandHandler<CreateLocationCommand, LocationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocationRepository _repository;
    
    public CreateLocationHandler(IUnitOfWork unitOfWork, ILocationRepository repository)
    {
        _unitOfWork = unitOfWork;
        _repository = repository;
    }
    public async Task<LocationDto> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        var location = new Location
        {
            Name = command.Name,
            Address = new Address
            {
                Street = command.Address.Street,
                BuildingNumber = command.Address.BuildingNumber,
                City = command.Address.City,
                PostalCode = command.Address.PostalCode
            }
        };
        
        if (await _repository.IsDuplicate(location: location, cancellationToken))
            throw new LocationAlreadyExistsException(location.Name, location.Address);
        
        _repository.Add(location);
        await _unitOfWork.SaveChanges(cancellationToken);
        
        var locationDto = new LocationDto(location.Id, location.Name, new AddressDto(
            location.Address.Street, location.Address.BuildingNumber,
            location.Address.City, location.Address.PostalCode));
        return locationDto;
    }
}
