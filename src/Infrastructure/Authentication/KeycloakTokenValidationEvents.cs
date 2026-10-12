using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Infrastructure.Authentication;

internal sealed class KeycloakTokenValidationEvents(HttpClient client, IOptions<KeycloakOptions> options, BookingUserSynchronizer users) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity is not ClaimsIdentity identity
            || !Guid.TryParse(principal.FindFirstValue("sub"), out var userId)
            || !string.Equals(principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase))
        {
            context.Fail("A verified Keycloak user is required.");
            return;
        }

        var token = context.SecurityToken switch
        {
            JsonWebToken jwt => jwt.EncodedToken,
            JwtSecurityToken jwt => jwt.RawData,
            _ => null
        };
        if (token is null)
        {
            context.Fail("An access token is required.");
            return;
        }

        // Check the live session on every request so logout invalidates already-issued tokens.
        using var body = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["client_id"] = options.Value.Audience,
            ["client_secret"] = options.Value.ClientSecret,
            ["token"] = token,
            ["token_type_hint"] = "access_token"
        });
        using var response = await client.PostAsync(
            $"{options.Value.ServerAuthority}/protocol/openid-connect/token/introspect", body, context.HttpContext.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            context.Fail("Token introspection failed.");
            return;
        }
        using var content = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted),
            cancellationToken: context.HttpContext.RequestAborted);
        if (!content.RootElement.TryGetProperty("active", out var active) || active.ValueKind != JsonValueKind.True)
        {
            context.Fail("The Keycloak session is no longer active.");
            return;
        }

        if (principal.FindFirstValue("realm_access") is { } realmAccess)
        {
            using var roles = JsonDocument.Parse(realmAccess);
            if (roles.RootElement.TryGetProperty("roles", out var roleNames))
                foreach (var role in roleNames.EnumerateArray())
                {
                    var name = role.GetString();
                    if (name is "Employee" or "Administrator")
                        identity.AddClaim(new Claim(ClaimTypes.Role, name));
                }
        }
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        await SynchronizeUser(principal, userId, context.HttpContext.RequestAborted);
    }

    private Task SynchronizeUser(ClaimsPrincipal principal, Guid userId, CancellationToken cancellationToken) =>
        users.Synchronize(userId,
            principal.FindFirstValue("given_name") ?? string.Empty,
            principal.FindFirstValue("family_name") ?? string.Empty,
            principal.FindFirstValue("email") ?? string.Empty,
            cancellationToken);
}
