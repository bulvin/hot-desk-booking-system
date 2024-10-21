using Domain.Locations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _dbContext;

    public LocationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Location location)
    {
        _dbContext.Add(location);
    }

    public void Delete(Location location)
    {
        _dbContext.Remove(location);
    }
    
    public async Task<Location?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
       return await _dbContext.Locations
            .Include(l => l.Desks)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }
    public async Task<bool> IsDuplicate(Location location, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations.AnyAsync(l =>
            l.Name == location.Name &&
            l.Address.Street == location.Address.Street &&
            l.Address.BuildingNumber == location.Address.BuildingNumber &&
            l.Address.City == location.Address.City &&
            l.Address.PostalCode == location.Address.PostalCode,
            cancellationToken: cancellationToken);
    }
}