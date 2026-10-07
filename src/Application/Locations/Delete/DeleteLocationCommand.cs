using Application.Interfaces.CQRS;
using MediatR;

namespace Application.Locations.Delete;

public record DeleteLocationCommand(Guid Id) : ICommand<Unit>;