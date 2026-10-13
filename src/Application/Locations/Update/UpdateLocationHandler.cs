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
        var proposed = new Location
        {
            Id = location.Id,
            Name = command.Name,
            Address = new Address
            {
                Street = command.Address.Street,
                BuildingNumber = command.Address.BuildingNumber,
                City = command.Address.City,
                PostalCode = command.Address.PostalCode
            }
        };

        if (await repository.IsDuplicate(proposed, cancellationToken))
            throw new LocationAlreadyExistsException(proposed.Name, proposed.Address);

        location.Name = proposed.Name;
        location.Address.Street = proposed.Address.Street;
        location.Address.BuildingNumber = proposed.Address.BuildingNumber;
        location.Address.City = proposed.Address.City;
        location.Address.PostalCode = proposed.Address.PostalCode;
        await unitOfWork.SaveChanges(cancellationToken);

        return new LocationDto(location.Id, location.Name, command.Address);
    }
}
