using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskHasActiveReservationException : HotDeskBookingException
{
    public Guid DeskId { get; }

    public DeskHasActiveReservationException(Guid deskId) 
        : base($"Cannot delete desk {deskId} as it has active reservations")
    {
        DeskId = deskId;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}