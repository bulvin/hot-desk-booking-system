using Domain.Locations;
using Infrastructure.Data;

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
}