using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UnitOfWork(AppDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task SaveChanges(CancellationToken cancellationToken = default)
    {
        UpdateEntities();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void UpdateEntities()
    {
        var entries = _dbContext
            .ChangeTracker
            .Entries<Entity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = entry.Entity.UpdatedAt = _dateTimeProvider.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = _dateTimeProvider.UtcNow;
                    break;
            }
        }
    }
}