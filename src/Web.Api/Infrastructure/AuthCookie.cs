using Application.Accounts;
using Application.Dtos;

namespace Web.Api.Infrastructure;

internal static class AuthCookie
{
    private const string Name = "hotdesk.refresh";

    public static string? Read(HttpContext context) => context.Request.Cookies[Name];

    public static void Set(HttpContext context, SessionDto session)
    {
        context.Response.Headers.CacheControl = "no-store";
        var options = Options(context);
        options.Expires = new DateTimeOffset(session.SessionExpiresAt, TimeSpan.Zero);
        context.Response.Cookies.Append(Name, session.RefreshToken, options);
    }

    public static void Clear(HttpContext context) => context.Response.Cookies.Delete(Name, Options(context));

    // Local HTTP development explicitly opts out; every other environment always sets Secure.
#pragma warning disable S2092
    private static CookieOptions Options(HttpContext context) => new()
    {
        HttpOnly = true,
        Secure = !(context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
            && context.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("Authentication:AllowInsecureCookies")),
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth"
    };
#pragma warning restore S2092
}
