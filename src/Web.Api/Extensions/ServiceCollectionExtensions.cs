using System.Globalization;
using System.Text.Json.Nodes;
using Infrastructure.Data.Converters;
using Microsoft.OpenApi;
using Web.Api.Infrastructure;

namespace Web.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddSwaggerGenWithAuth(IConfiguration configuration)
        {
            services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new OpenApiInfo { Title = "HotDesk Booking API", Version = "v1" });

                o.MapType<DateOnly>(() => new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "date",
                    Default = JsonValue.Create(DateOnly.FromDateTime(DateTime.Today).ToString(DateOnlyJsonConverter.Format, CultureInfo.InvariantCulture))
                });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Paste the access token returned by POST /api/auth/login."
                };
                o.AddSecurityDefinition("Bearer", securityScheme);
                o.OperationFilter<BearerSecurityOperationFilter>();
                o.OperationFilter<RateLimitOperationFilter>();
                o.EnableAnnotations();
            });

            return services;
        }
    }
}
