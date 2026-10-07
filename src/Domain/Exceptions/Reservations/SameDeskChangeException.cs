using System.Net;

namespace Domain.Exceptions.Reservations;

public class SameDeskChangeException(Guid deskId)
    : HotDeskBookingException($"Cannot change reservation to the same desk {deskId}")
{
    public Guid Id { get; } = deskId;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}