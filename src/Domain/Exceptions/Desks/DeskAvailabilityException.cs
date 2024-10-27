using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskAvailabilityException : HotDeskBookingException
{
    public bool IsAvailability { get; }

    public DeskAvailabilityException(bool isAvailability) : base(
        $"Desk is already {(isAvailability ? "available" : "unavailable")}")
    {
        IsAvailability = isAvailability;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}