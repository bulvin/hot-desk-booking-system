using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskHasActiveReservationException(Guid deskId)
    : HotDeskBookingException($"Cannot delete desk {deskId} as it has reservation history")
{
    public Guid DeskId { get; } = deskId;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}