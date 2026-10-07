using System.Net;

namespace Domain.Exceptions.Users;

public class InvalidCredentialsException() : HotDeskBookingException("Invalid email or password")
{
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}