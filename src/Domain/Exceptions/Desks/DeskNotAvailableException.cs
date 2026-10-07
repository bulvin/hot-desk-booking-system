using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskNotAvailableException(Guid deskId)
    : HotDeskBookingException($"Desk {deskId} is not available for booking")
{
    public Guid Id { get; } = deskId;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}