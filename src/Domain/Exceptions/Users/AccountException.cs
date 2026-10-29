using System.Net;

namespace Domain.Exceptions.Users;

public sealed class AccountException(HttpStatusCode status, string message) : HotDeskBookingException(message)
{
    public override HttpStatusCode HttpStatusCode => status;
}
