using System.Security.Claims;
using Domain.Exceptions;
using Domain.Exceptions.Users;
using Domain.Users;
using Microsoft.AspNetCore.Http;

namespace Application;

public static class HttpContextExtensions
{
    public static Guid GetUserId(this IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null)
            throw new UserContextNotFoundException();

        var id = user.FindFirstValue(ClaimTypes.NameIdentifier!);

        return Guid.TryParse(id, out var parsedId)
            ? parsedId
            : throw new InvalidUserIdException(id);
    }

    public static bool HasRole(this IHttpContextAccessor httpContextAccessor, UserRole role)
    {
        return httpContextAccessor.HttpContext?.User.IsInRole(role.ToString()) ?? false;
    }
}