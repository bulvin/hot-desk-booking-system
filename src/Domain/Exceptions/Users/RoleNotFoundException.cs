using System.Net;

namespace Domain.Exceptions.Users;

public class RoleNotFoundException : HotDeskBookingException
{
    public string Name { get; }

    public RoleNotFoundException(string roleName) 
        : base($"Role {roleName} was not found")
    {
        Name = roleName;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}