using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Authentication;

public static class HttpContextExtensions
{
    public static Guid GetUserId(this IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null)
            throw new ApplicationException("User not present");

        var id = user.FindFirstValue(ClaimTypes.NameIdentifier!);

        return Guid.TryParse(id, out var parsedId)
            ? parsedId
            : throw new ApplicationException("Invalid User ID format");
    }
}