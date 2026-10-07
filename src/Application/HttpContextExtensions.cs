using System.Security.Claims;
using Domain.Exceptions.Users;
using Domain.Users;
using Microsoft.AspNetCore.Http;

namespace Application;

public static class HttpContextExtensions
{
    extension(IHttpContextAccessor httpContextAccessor)
    {
        public Guid GetUserId()
        {
            var user = httpContextAccessor.HttpContext?.User
                ?? throw new UserContextNotFoundException();

            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(id, out var parsedId)
                ? parsedId
                : throw new InvalidUserIdException(id);
        }

        public bool HasRole(UserRole role)
        {
            return httpContextAccessor.HttpContext?.User.IsInRole(role.ToString()) ?? false;
        }
    }
}