namespace Web.Api.Infrastructure;

internal static class ProblemDetailsConfiguration
{
    internal static void Customize(ProblemDetailsContext context)
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.PathBase}{context.HttpContext.Request.Path}";
    }
}
