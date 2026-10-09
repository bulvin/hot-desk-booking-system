using Application.Interfaces.CQRS;
using Unit = Mediator.Unit;

namespace Application.Reservations.ChangeDesk;

public record ChangeDeskCommand(Guid Id, Guid DeskId) : ICommand<Unit>;