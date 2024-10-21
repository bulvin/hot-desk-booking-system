using Domain.Desks;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DeskRepository : IDeskRepository
{
    private readonly AppDbContext _dbContext;

    public DeskRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Desk desk)
    {
        _dbContext.Desks.Add(desk);
    }

    public void Delete(Desk desk)
    {
        _dbContext.Desks.Remove(desk);
    }

    public async Task<Desk?> GetByIdAndLocation(Guid id, Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Desks
            .Where(d => d.Id == id && d.LocationId == locationId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}