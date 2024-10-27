using System.Net;

namespace Domain.Exceptions;

public abstract class HotDeskBookingException : Exception
{
    public abstract HttpStatusCode HttpStatusCode { get; }
    protected HotDeskBookingException(string message) : base(message)
    {
    }
}