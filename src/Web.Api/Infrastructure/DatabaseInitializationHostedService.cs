using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Infrastructure;

internal sealed class DatabaseInitializationHostedService(IServiceScopeFactory scopeFactory,
    ILogger<DatabaseInitializationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        logger.LogInformation("Applying pending database migrations.");
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database initialization completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
