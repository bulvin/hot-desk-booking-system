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
        var address = new Address(command.Address.Street, command.Address.BuildingNumber,
            command.Address.City, command.Address.PostalCode);

        if (await repository.IsDuplicate(command.Name, address, cancellationToken: cancellationToken))
            throw new LocationAlreadyExistsException(command.Name, address);

        var location = new Location { Name = command.Name, Address = address };
        repository.Add(location);
        await unitOfWork.SaveChanges(cancellationToken);

        return new LocationDto(location.Id, location.Name, new AddressDto(
            address.Street, address.BuildingNumber, address.City, address.PostalCode));
    }
}
