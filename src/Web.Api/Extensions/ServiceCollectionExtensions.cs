using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Web.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(o =>
        {
            o.MapType<DateOnly>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "date",
                Example = new OpenApiString("2024-01-01")
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

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    []
                }
            };

            o.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }
}