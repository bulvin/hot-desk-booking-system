using Application.Interfaces.CQRS;
using Unit = Mediator.Unit;

namespace Application.Desks.ChangeAvailability;

public record ChangeDeskAvailabilityCommand(Guid Id, Guid LocationId, bool IsAvailable) : ICommand<Unit>;