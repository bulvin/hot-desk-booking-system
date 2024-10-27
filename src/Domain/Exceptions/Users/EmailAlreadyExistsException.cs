using System.Net;

namespace Domain.Exceptions.Users;

public class EmailAlreadyExistsException : HotDeskBookingException
{
    public string Email { get; }

    public EmailAlreadyExistsException(string email) 
        : base($"Email {email} is already registered")
    {
        Email = email;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}