using System.Net;

namespace Domain.Exceptions.Reservations;

public class SameDeskChangeException : HotDeskBookingException
{
    public Guid Id { get; }

    public SameDeskChangeException(Guid deskId) 
        : base($"Cannot change reservation to the same desk {deskId}")
    {
        Id = deskId;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}