using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Web.Api.Infrastructure;

namespace Web.Api.Extensions;

internal static class RateLimitingExtensions
{
    internal static void AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiRateLimitingOptions>()
            .Bind(configuration.GetSection("RateLimiting"))
            .Validate(options => options.IpPermitLimit > 0 && options is { UserPermitLimit: > 0, WritePermitLimit: > 0, WindowSeconds: > 0, AuthPermitLimit: > 0 },
                "Rate limiting permit limits and window duration must be positive.")
            .ValidateOnStart();
        services.AddSingleton<IpRateLimiter>();
        services.AddRateLimiter(_ => { });
        services.AddOptions<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>()
            .Configure<IOptions<ApiRateLimitingOptions>>((options, settings) =>
            {
                options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Value.AuthPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Value.WindowSeconds),
                        QueueLimit = 0
                    }));
                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    CreateUserLimiter(settings.Value, writesOnly: false),
                    CreateUserLimiter(settings.Value, writesOnly: true));
                options.OnRejected = (context, _) => RateLimitRejection.WriteAsync(context.HttpContext, context.Lease);
            });
    }

    internal static IApplicationBuilder UseApiIpRateLimiting(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        using var lease = context.RequestServices.GetRequiredService<IpRateLimiter>().Acquire(context);
        if (!lease.IsAcquired)
        {
            await RateLimitRejection.WriteAsync(context, lease);
            return;
        }
        await next(context);
    });

    private static PartitionedRateLimiter<HttpContext> CreateUserLimiter(ApiRateLimitingOptions options, bool writesOnly) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var method = context.Request.Method;
            var isWrite = HttpMethods.IsPost(method) || HttpMethods.IsPut(method)
                || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
            if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) || context.User.Identity?.IsAuthenticated != true
                || userId is null || (writesOnly && !isWrite))
                return RateLimitPartition.GetNoLimiter("unlimited");

            return RateLimitPartition.GetFixedWindowLimiter($"user:{userId}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = writesOnly ? options.WritePermitLimit : options.UserPermitLimit,
                Window = TimeSpan.FromSeconds(options.WindowSeconds),
                QueueLimit = 0
            });
        });
}
