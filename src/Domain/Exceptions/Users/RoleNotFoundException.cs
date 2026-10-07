using System.Net;

namespace Domain.Exceptions.Users;

public class RoleNotFoundException(string roleName) : HotDeskBookingException($"Role {roleName} was not found")
{
    public string Name { get; } = roleName;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}