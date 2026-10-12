using System.Security.Claims;
using Application.Dtos;
using Application.Interfaces;
using Domain.Exceptions.Users;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Authentication;

internal sealed class UserContext : IUserContext
{
    private readonly ProfileDto _user;

    public UserContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new UserContextNotFoundException();
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
            throw new InvalidUserIdException(userId);

        _user = new ProfileDto(
            id,
            user.FindFirstValue("email") ?? string.Empty,
            user.FindFirstValue("given_name") ?? string.Empty,
            user.FindFirstValue("family_name") ?? string.Empty,
            user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct(StringComparer.Ordinal).ToArray(),
            string.Equals(user.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase));
    }

    public ProfileDto GetCurrentUser() => _user;
}
