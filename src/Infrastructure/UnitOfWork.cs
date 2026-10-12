using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class UnitOfWork(AppDbContext dbContext, IDateTimeProvider dateTimeProvider) : IUnitOfWork
{
    public async Task SaveChanges(CancellationToken cancellationToken = default)
    {
        UpdateEntities();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void UpdateEntities()
    {
        var now = dateTimeProvider.UtcNow;
        var entries = dbContext
            .ChangeTracker
            .Entries<Entity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
