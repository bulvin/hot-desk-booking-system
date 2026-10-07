using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskInLocationAlreadyExistsException(string name)
    : HotDeskBookingException($"Desk with name {name} already exists")
{
    public string Name { get; } = name;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}