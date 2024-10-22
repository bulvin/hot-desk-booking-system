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
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        return services;
    }

    private static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = Jwt.SecurityKey(configuration["Jwt:Key"]!),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };
        });
        
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<ITokenProvider, Jwt>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        
        services.AddHttpContextAccessor();

        return services;
    }
}