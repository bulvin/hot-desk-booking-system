using System.Security.Claims;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authentication;

internal sealed class LocalTokenValidationEvents(AppDbContext db, IDateTimeProvider clock) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity is not ClaimsIdentity identity
            || !Guid.TryParse(principal.FindFirstValue("sub"), out var userId)
            || !Guid.TryParse(principal.FindFirstValue("sid"), out var sessionId)
            || !await db.AuthSessions.AnyAsync(session => session.Id == sessionId && session.UserId == userId
                && !session.IsRevoked && session.ExpiresAt > clock.UtcNow
                && db.Users.Any(user => user.Id == userId && user.IsActive), context.HttpContext.RequestAborted))
        {
            context.Fail("Invalid or expired session.");
            return;
        }
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        foreach (var role in principal.FindAll("role").ToArray())
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Value));
    }
}
