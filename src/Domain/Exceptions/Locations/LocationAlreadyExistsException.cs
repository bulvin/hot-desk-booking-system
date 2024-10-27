using System.Net;
using Domain.Locations;

namespace Domain.Exceptions.Locations;

public class LocationAlreadyExistsException : HotDeskBookingException
{
    public string Name { get; }
    public Address Address { get;  }

    public LocationAlreadyExistsException(string name, Address address) : base($"Location with name {name} and address {address.Street + ' ' + address.BuildingNumber + ' ' + address.PostalCode + ' ' + address.City} already exists")
    {
        Name = name;
        Address = address;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}