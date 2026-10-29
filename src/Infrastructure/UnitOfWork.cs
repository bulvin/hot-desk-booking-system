using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class UnitOfWork(AppDbContext dbContext, IDateTimeProvider dateTimeProvider) : IUnitOfWork
{
    public async Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await operation(cancellationToken);
        await SaveChanges(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

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
