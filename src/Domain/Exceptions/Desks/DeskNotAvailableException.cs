using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskNotAvailableException : HotDeskBookingException
{
    public Guid Id { get; }

    public DeskNotAvailableException(Guid deskId) 
        : base($"Desk {deskId} is not available for booking")
    {
        Id = deskId;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}
