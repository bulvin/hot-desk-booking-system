using System.Security.Claims;
using Domain.Users;
using Microsoft.AspNetCore.Http;

namespace Application;

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

    public static bool HasRole(this IHttpContextAccessor httpContextAccessor, UserRole role)
    {
        return httpContextAccessor.HttpContext?.User.IsInRole(role.ToString()) ?? false;
    }
}