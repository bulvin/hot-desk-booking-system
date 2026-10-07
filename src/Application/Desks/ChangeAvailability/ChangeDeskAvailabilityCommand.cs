using Application.Interfaces.CQRS;
using MediatR;

namespace Application.Desks.ChangeAvailability;

public record ChangeDeskAvailabilityCommand(Guid Id, Guid LocationId, bool IsAvailable) : ICommand<Unit>;