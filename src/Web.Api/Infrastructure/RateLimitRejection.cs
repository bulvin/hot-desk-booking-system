using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Infrastructure;

internal static class RateLimitRejection
{
    internal static async ValueTask WriteAsync(HttpContext context, RateLimitLease lease)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.Response.Headers.RetryAfter = Math.Max(1, Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Type = "https://www.rfc-editor.org/rfc/rfc6585#section-4",
            Detail = "Request limit exceeded. Retry after the interval specified by the Retry-After header."
        };
        var problemContext = new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem };
        var service = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        if (!await service.TryWriteAsync(problemContext))
        {
            ProblemDetailsConfiguration.Customize(problemContext);
            await context.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
