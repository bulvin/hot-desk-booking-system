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
}