using Application.Interfaces.CQRS;
using Unit = Mediator.Unit;

namespace Application.Locations.Delete;

public record DeleteLocationCommand(Guid Id) : ICommand<Unit>;