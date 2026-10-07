using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskNotFoundException(Guid id) : HotDeskBookingException($"Desk with ID {id} was not found")
{
    public Guid Id { get; } = id;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}