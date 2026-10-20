using Domain.Locations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LocationRepository(AppDbContext dbContext) : ILocationRepository
{
    public async Task<(IReadOnlyList<Location> Items, int Total)> GetPage(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Locations.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((int)Math.Min(((long)page - 1) * pageSize, int.MaxValue)).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public void Add(Location location)
    {
        dbContext.Add(location);
    }

    public void Delete(Location location)
    {
        dbContext.Remove(location);
    }

    public async Task<Location?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Locations
             .Include(l => l.Desks)
             .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<bool> IsDuplicate(string name, Address address, Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        var locations = dbContext.Locations.Where(l => l.Name == name);
        if (excludedId.HasValue)
            locations = locations.Where(l => l.Id != excludedId.Value);

        return await locations.AnyAsync(l => l.Address == address, cancellationToken);
    }
}
