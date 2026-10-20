using System.Diagnostics;
using System.Security.Claims;
using Serilog;
using Serilog.Context;

namespace Web.Api.Extensions;

internal static class RequestLoggingExtensions
{
    extension(WebApplication app)
    {
        internal void UseRequestLogging()
        {
            app.Use(async (context, next) =>
            {
                using var traceScope = LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
                using var requestScope = LogContext.PushProperty("RequestId", context.TraceIdentifier);
                await next(context);
            });
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000}ms";

                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (userId is not null)
                    {
                        diagnosticContext.Set("UserId", userId);
                    }
                };
            });
        }
    }
}
