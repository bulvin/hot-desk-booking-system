using System.Net;
using Domain.Locations;

namespace Domain.Exceptions.Locations;

public class LocationAlreadyExistsException(string name, Address address)
    : HotDeskBookingException(CreateMessage(name, address))
{
    public string Name { get; } = name;
    public Address Address { get; } = address;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;

    private static string CreateMessage(string name, Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return $"Location with name {name} and address {address.Street} {address.BuildingNumber} {address.PostalCode} {address.City} already exists";
    }
}