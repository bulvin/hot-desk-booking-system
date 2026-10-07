using System.Net;

namespace Domain.Exceptions.Locations;

public class LocationHasDesksException(Guid locationId)
    : HotDeskBookingException($"Cannot delete location {locationId} as it has existing desks")
{
    public Guid Id { get; } = locationId;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}