using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Web.Api.Extensions;

internal static class HealthCheckExtensions
{
    internal static void UsePrivateHealthChecks(this IApplicationBuilder app)
    {
        var port = app.ApplicationServices.GetRequiredService<IConfiguration>().GetValue<int?>("HealthChecks:Port");
        if (port.HasValue)
            app.Use(async (context, next) =>
            {
                var isHealth = context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
                if (isHealth != (context.Connection.LocalPort == port.Value))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                await next(context);
            });
        app.UseHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.UseHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
    }
}
