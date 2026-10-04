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

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
       return services
           .AddServices()
           .AddAuthorization()
           .AddAuthentication(configuration)
           .AddDatabase(configuration);
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
       services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
       services.AddScoped<IUnitOfWork, UnitOfWork>();
       services.AddScoped<ILocationRepository, LocationRepository>();
       services.AddScoped<IDeskRepository, DeskRepository>();
       services.AddScoped<IReservationRepository, ReservationRepository>();
       services.AddScoped<IUserRepository, UserRepository>();
       
       return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
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

    private static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
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
                IssuerSigningKey = Jwt.SecurityKey(jwtKey),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };
        });
        
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(options => options.Expires > 0, "Jwt:Expires must be positive.")
            .ValidateOnStart();
        services.AddSingleton<ITokenProvider, Jwt>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        
        services.AddHttpContextAccessor();
        
        return services;
    }

    private static IServiceCollection AddAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.Admin, policy =>
            {
                policy.RequireClaim(ClaimTypes.Role, PolicyNames.Admin);
            })
            .AddPolicy(PolicyNames.Employee, policy =>
            {
                policy.RequireClaim(ClaimTypes.Role, PolicyNames.Employee);
            });

        return services;
    }
}
