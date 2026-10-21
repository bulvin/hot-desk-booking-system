using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Web.Api.Infrastructure;

internal sealed class RateLimitOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses?.TryGetValue("429", out var response) != true || response is not OpenApiResponse concreteResponse)
            return;

        concreteResponse.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.Ordinal);
        concreteResponse.Headers["Retry-After"] = new OpenApiHeader
        {
            Description = "Seconds to wait before retrying the request.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
        };
    }
}
