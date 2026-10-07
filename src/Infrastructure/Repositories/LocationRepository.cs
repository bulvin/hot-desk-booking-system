using Domain.Locations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LocationRepository(AppDbContext dbContext) : ILocationRepository
{
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
    public async Task<bool> IsDuplicate(Location location, CancellationToken cancellationToken = default)
    {
        return await dbContext.Locations.AnyAsync(l =>
            l.Name == location.Name &&
            l.Address.Street == location.Address.Street &&
            l.Address.BuildingNumber == location.Address.BuildingNumber &&
            l.Address.City == location.Address.City &&
            l.Address.PostalCode == location.Address.PostalCode,
            cancellationToken: cancellationToken);
    }
}