using System.Net;

namespace Domain.Exceptions.Locations;

public class LocationHasDesksException : HotDeskBookingException
{
    public Guid Id { get; }

    public LocationHasDesksException(Guid locationId) 
        : base($"Cannot delete location {locationId} as it has existing desks")
    {
        Id = locationId;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}