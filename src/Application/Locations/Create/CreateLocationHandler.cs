using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Exceptions.Locations;
using Domain.Locations;

namespace Application.Locations.Create;

public class CreateLocationHandler(IUnitOfWork unitOfWork, ILocationRepository repository)
    : ICommandHandler<CreateLocationCommand, LocationDto>
{
    public async ValueTask<LocationDto> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
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

        if (await repository.IsDuplicate(location: location, cancellationToken))
            throw new LocationAlreadyExistsException(location.Name, location.Address);

        repository.Add(location);
        await unitOfWork.SaveChanges(cancellationToken);

        var locationDto = new LocationDto(location.Id, location.Name, new AddressDto(
            location.Address.Street, location.Address.BuildingNumber,
            location.Address.City, location.Address.PostalCode));
        return locationDto;
    }
}