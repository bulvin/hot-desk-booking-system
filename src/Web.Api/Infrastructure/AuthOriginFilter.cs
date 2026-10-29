namespace Web.Api.Infrastructure;

internal sealed class AuthOriginFilter(IConfiguration configuration) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        var origin = request.Headers.Origin.ToString();
        var expected = configuration["Authentication:BrowserOrigin"];
        var sameOrigin = $"{request.Scheme}://{request.Host}";
        if (request.Headers["Sec-Fetch-Site"] == "cross-site"
            || (!string.IsNullOrEmpty(origin) && !string.Equals(origin, expected, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(origin, sameOrigin, StringComparison.OrdinalIgnoreCase)))
            return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Request origin is not allowed.");
        // Browser cookie operations require an Origin; command-line clients may omit it.
        if (string.IsNullOrEmpty(origin) && request.Headers.ContainsKey("Sec-Fetch-Site"))
            return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Request origin is required.");
        return await next(context);
    }
}
