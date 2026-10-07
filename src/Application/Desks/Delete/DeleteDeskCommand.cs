using Application.Interfaces.CQRS;
using MediatR;

namespace Application.Desks.Delete;

public record DeleteDeskCommand(Guid LocationId, Guid DeskId) : ICommand<Unit>;