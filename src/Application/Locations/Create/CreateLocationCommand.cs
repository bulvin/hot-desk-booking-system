using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Locations.Create;

public record CreateLocationCommand(string Name, AddressDto Address) : ICommand<LocationDto>;