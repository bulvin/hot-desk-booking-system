using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Locations.Update;

public record UpdateLocationCommand(Guid Id, string Name, AddressDto Address) : ICommand<LocationDto>;
