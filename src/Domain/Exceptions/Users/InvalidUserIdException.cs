using System.Net;

namespace Domain.Exceptions.Users;

public class InvalidUserIdException : HotDeskBookingException
{
    public string? Id { get; }

    public InvalidUserIdException(string? id) 
        : base($"Invalid user identifier format: {id ?? "null"}")
    {
        Id = id;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Unauthorized;
}