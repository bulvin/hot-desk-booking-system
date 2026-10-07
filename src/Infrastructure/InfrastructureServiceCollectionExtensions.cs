using System.Security.Claims;
using Application.Interfaces;
using Domain;
using Domain.Desks;
using Domain.Locations;
using Domain.Reservations;
using Domain.Users;
using Infrastructure.Authentication;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            return services
                .AddServices()
                .AddAuthorization()
                .AddAuthentication(configuration)
                .AddDatabase(configuration);
        }

        private IServiceCollection AddServices()
        {
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IDeskRepository, DeskRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }

        private IServiceCollection AddDatabase(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Database");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:Database is required. Configure it using .NET User Secrets for development.");
            }

            return services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        private IServiceCollection AddAuthentication(IConfiguration configuration)
        {
            var jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey) || System.Text.Encoding.ASCII.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key must contain at least 32 bytes for HS256. Configure Jwt:Key using .NET User Secrets for development.");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = JwtTokenProvider.SecurityKey(jwtKey),
                    ValidIssuer = configuration["Jwt:Issuer"] ?? JwtOptions.DefaultIssuer,
                    ValidAudience = configuration["Jwt:Audience"] ?? JwtOptions.DefaultAudience,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection("Jwt"))
                .Validate(options => options.Expires > 0, "Jwt:Expires must be positive.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required.")
                .ValidateOnStart();
            services.AddSingleton<ITokenProvider, JwtTokenProvider>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            services.AddHttpContextAccessor();

            return services;
        }

        private IServiceCollection AddAuthorization()
        {
            services.AddAuthorizationBuilder()
                .AddPolicy(PolicyNames.Admin, policy => policy.RequireClaim(ClaimTypes.Role, PolicyNames.Admin))
                .AddPolicy(PolicyNames.Employee, policy => policy.RequireClaim(ClaimTypes.Role, PolicyNames.Employee));

            return services;
        }
    }
}