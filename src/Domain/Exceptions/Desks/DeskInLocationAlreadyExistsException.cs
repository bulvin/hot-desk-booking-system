using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskInLocationAlreadyExistsException : HotDeskBookingException
{
    public string Name { get; }
    public DeskInLocationAlreadyExistsException(string name) : base($"Desk with name {name} already exists")
    {
        Name = name;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}