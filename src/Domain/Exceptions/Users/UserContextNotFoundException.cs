using System.Net;

namespace Domain.Exceptions.Users;

public class UserContextNotFoundException : HotDeskBookingException
{
    public UserContextNotFoundException() 
        : base("User context not found - user is not authenticated")
    {
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Unauthorized;
}