using System.Net;

namespace Domain.Exceptions.Locations;

public class LocationNotFoundException : HotDeskBookingException
{
    public Guid Id { get; }
    public LocationNotFoundException(Guid id) : base($"Location id {id} was not found")
    {
        Id = id;
    }
    
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}