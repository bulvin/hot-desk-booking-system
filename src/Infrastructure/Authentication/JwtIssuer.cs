using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Accounts;
using Application.Interfaces;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

internal sealed class JwtIssuer(UserManager<User> users, IOptions<JwtOptions> options, IDateTimeProvider clock)
{
    public async Task<SessionDto> Issue(User account, AuthSession session, string refreshToken, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var expires = new[] { clock.UtcNow.AddMinutes(settings.AccessTokenMinutes), session.ExpiresAt }.Min();
        var claims = new List<Claim>
        {
            new("sub", account.Id.ToString()), new("sid", session.Id.ToString()),
            new("jti", Guid.NewGuid().ToString()), new("email", account.Email ?? string.Empty),
            new("given_name", account.FirstName), new("family_name", account.LastName), new("email_verified", "false")
        };
        claims.AddRange((await users.GetRolesAsync(account)).Select(role => new Claim("role", role)));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, clock.UtcNow, expires,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new SessionDto(new JwtSecurityTokenHandler().WriteToken(token), expires, refreshToken, session.ExpiresAt);
    }
}
