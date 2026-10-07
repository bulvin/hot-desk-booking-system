using System.Net;

namespace Domain.Exceptions.Users;

public class UserContextNotFoundException()
    : HotDeskBookingException("User context not found - user is not authenticated")
{
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Unauthorized;
}