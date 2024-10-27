using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskNotFoundException : HotDeskBookingException
{
    public Guid Id { get; }
    public DeskNotFoundException(Guid id) : base($"Desk with ID {id} was not found")
    {
        Id = id;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}