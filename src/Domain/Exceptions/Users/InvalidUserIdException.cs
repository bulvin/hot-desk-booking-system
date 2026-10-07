using System.Net;

namespace Domain.Exceptions.Users;

public class InvalidUserIdException(string? id)
    : HotDeskBookingException($"Invalid user identifier format: {id ?? "null"}")
{
    public string? Id { get; } = id;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Unauthorized;
}