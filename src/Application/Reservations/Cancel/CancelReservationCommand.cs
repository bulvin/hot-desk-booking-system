using Application.Interfaces.CQRS;
using Unit = Mediator.Unit;

namespace Application.Reservations.Cancel;

public record CancelReservationCommand(Guid Id) : ICommand<Unit>;
