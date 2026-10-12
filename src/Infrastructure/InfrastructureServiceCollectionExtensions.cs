using System.Security.Claims;
using Application.Interfaces;
using Domain;
using Domain.Desks;
using Domain.Locations;
using Domain.Reservations;
using Infrastructure.Authentication;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddLogging();
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IDeskRepository, DeskRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddHttpContextAccessor();
            services.AddScoped<IUserContext, UserContext>();
            services.AddScoped<BookingUserSynchronizer>();
            services.AddHttpClient<KeycloakTokenValidationEvents>();

            var connection = configuration.GetConnectionString("Database");
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("ConnectionStrings:Database is required.");
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection));

            services.AddOptions<KeycloakOptions>().Bind(configuration.GetSection("Keycloak"))
                .Validate(options => Uri.TryCreate(options.Authority, UriKind.Absolute, out var uri)
                    && uri.Scheme is "http" or "https", "Keycloak:Authority must be an absolute HTTP(S) realm URL.")
                .Validate(options => options.BackchannelAuthority is null
                    || Uri.TryCreate(options.BackchannelAuthority, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https",
                    "Keycloak:BackchannelAuthority must be an absolute HTTP(S) realm URL.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Keycloak:Audience is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.ClientSecret),
                    "Keycloak:ClientSecret is required for live token introspection.")
                .ValidateOnStart();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<KeycloakOptions>>((options, keycloakOptions) =>
                {
                    var keycloak = keycloakOptions.Value;
                    options.Authority = keycloak.Authority.TrimEnd('/');
                    options.MetadataAddress = $"{keycloak.ServerAuthority}/.well-known/openid-configuration";
                    options.Audience = keycloak.Audience;
                    options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
                    options.MapInboundClaims = false;
                    options.IncludeErrorDetails = false;
                    options.EventsType = typeof(KeycloakTokenValidationEvents);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = keycloak.Authority.TrimEnd('/'),
                        ValidAudience = keycloak.Audience,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        NameClaimType = "preferred_username",
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorizationBuilder()
                .AddPolicy(PolicyNames.Admin, policy => policy.RequireRole(PolicyNames.Admin))
                .AddPolicy(PolicyNames.Employee, policy => policy.RequireRole(PolicyNames.Employee));
            return services;
        }
    }
}
