using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Exceptions.Locations;
using Domain.Locations;

namespace Application.Locations.Update;

public class UpdateLocationHandler(ILocationRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateLocationCommand, LocationDto>
{
    public async ValueTask<LocationDto> Handle(UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await repository.GetById(command.Id, cancellationToken)
                       ?? throw new LocationNotFoundException(command.Id);
        var address = new Address(command.Address.Street, command.Address.BuildingNumber,
            command.Address.City, command.Address.PostalCode);

        if (await repository.IsDuplicate(command.Name, address, location.Id, cancellationToken))
            throw new LocationAlreadyExistsException(command.Name, address);

        location.Name = command.Name;
        location.Address = address;
        await unitOfWork.SaveChanges(cancellationToken);

        return new LocationDto(location.Id, location.Name, command.Address);
    }
}
