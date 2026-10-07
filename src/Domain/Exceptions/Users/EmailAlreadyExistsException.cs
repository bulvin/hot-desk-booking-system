using System.Net;

namespace Domain.Exceptions.Users;

public class EmailAlreadyExistsException(string email) : HotDeskBookingException($"Email {email} is already registered")
{
    public string Email { get; } = email;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}