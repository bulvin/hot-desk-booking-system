using Application.Interfaces.CQRS;
using MediatR;

namespace Application.Reservations.ChangeDesk;

public record ChangeDeskCommand(Guid Id, Guid DeskId) : ICommand<Unit>;