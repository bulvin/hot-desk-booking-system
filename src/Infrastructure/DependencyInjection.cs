using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
       return services
           .AddServices()
           .AddDatabase(configuration);
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
       services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
       services.AddScoped<IUnitOfWork, UnitOfWork>();
       return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        return services;
    }
}