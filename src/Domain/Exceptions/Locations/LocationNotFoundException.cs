using System.Net;

namespace Domain.Exceptions.Locations;

public class LocationNotFoundException(Guid id) : HotDeskBookingException($"Location id {id} was not found")
{
    public Guid Id { get; } = id;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}