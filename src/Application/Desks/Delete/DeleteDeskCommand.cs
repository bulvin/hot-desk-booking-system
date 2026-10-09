using Application.Interfaces.CQRS;
using Unit = Mediator.Unit;

namespace Application.Desks.Delete;

public record DeleteDeskCommand(Guid LocationId, Guid DeskId) : ICommand<Unit>;