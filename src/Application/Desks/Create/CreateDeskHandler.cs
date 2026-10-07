using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Locations;
using Domain.Locations;

namespace Application.Desks.Create;

public class CreateDeskHandler(
    IUnitOfWork unitOfWork,
    IDeskRepository deskRepository,
    ILocationRepository locationRepository)
    : ICommandHandler<CreateDeskCommand, DeskDto>
{
    public async Task<DeskDto> Handle(CreateDeskCommand command, CancellationToken cancellationToken)
    {
        var location = await locationRepository.GetById(command.LocationId, cancellationToken)
                       ?? throw new LocationNotFoundException(command.LocationId);

        var desk = new Desk
        {
            Name = command.Name,
            Description = command.Description,
            LocationId = command.LocationId
        };

        if (location.Desks.Any(d => string.Equals(d.Name, desk.Name, StringComparison.Ordinal)))
            throw new DeskInLocationAlreadyExistsException(command.Name);

        deskRepository.Add(desk);

        await unitOfWork.SaveChanges(cancellationToken);

        return new DeskDto(
            desk.Id,
            desk.Name,
            desk.Description,
            desk.LocationId,
            desk.IsAvailable);
    }
}