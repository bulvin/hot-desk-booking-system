using System.Globalization;
using System.Text.Json.Nodes;
using Infrastructure.Data.Converters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;

namespace Web.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddSwaggerGenWithAuth()
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
                    Name = "JWT Authentication",
                    Description = "Enter your JWT token",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                };

                o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

                o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document),
                        []
                    }
                });
                o.EnableAnnotations();
            });

            return services;
        }
    }
}
