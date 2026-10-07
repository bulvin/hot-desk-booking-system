using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Extensions;

internal static class MigrationExtensions
{
    extension(WebApplication app)
    {
        internal async Task EnsureDatabaseCreated()
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            await DbSeeder.SeedData(db);
        }
    }
}