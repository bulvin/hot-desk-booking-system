using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskAvailabilityException(bool isAvailability)
    : HotDeskBookingException($"Desk is already {(isAvailability ? "available" : "unavailable")}")
{
    public bool IsAvailability { get; } = isAvailability;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}