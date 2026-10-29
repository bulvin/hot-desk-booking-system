using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Health;

public static class HealthServiceCollectionExtensions
{
    public static IServiceCollection AddDependencyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseReadinessCheck>("database", tags: ["ready"], timeout: TimeSpan.FromSeconds(5));
        return services;
    }
}
