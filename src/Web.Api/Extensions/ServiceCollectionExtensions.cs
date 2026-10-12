using System.Globalization;
using System.Text.Json.Nodes;
using Infrastructure.Data.Converters;
using Microsoft.OpenApi;

namespace Web.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddSwaggerGenWithAuth(IConfiguration configuration)
        {
            var authority = (configuration["Keycloak:Authority"] ?? "http://localhost:8090/realms/hotdesk").TrimEnd('/');
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
                    Type = SecuritySchemeType.OAuth2,
                    Description = "Log in through Keycloak using Authorization Code + PKCE.",
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                            TokenUrl = new Uri($"{authority}/protocol/openid-connect/token"),
                            Scopes = new Dictionary<string, string>(StringComparer.Ordinal)
                            {
                                ["openid"] = "OpenID Connect",
                                ["profile"] = "User profile",
                                ["email"] = "Verified email"
                            }
                        }
                    }
                };

                o.AddSecurityDefinition("Keycloak", securityScheme);

                o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Keycloak", document),
                        ["openid", "profile", "email"]
                    }
                });
                o.EnableAnnotations();
            });

            return services;
        }
    }
}
