using System.Net;

namespace Domain.Exceptions;

public abstract class HotDeskBookingException(string message) : Exception(message)
{
    public abstract HttpStatusCode HttpStatusCode { get; }
}