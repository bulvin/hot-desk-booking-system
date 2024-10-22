using Domain.Desks;
using Domain.Reservations;
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

    public async Task<bool> Exists(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Desks.AnyAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Desk?> GetByIdAndLocation(Guid id, Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Desks
            .Where(d => d.Id == id && d.LocationId == locationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Desk?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Desks.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<(List<Desk> Desks, int Count)> GetDesksByLocation(Guid locationId, bool? isAvailable, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Desks
            .Include(d => d.Reservations) 
            .Where(d => d.LocationId == locationId); 
  
        if (isAvailable.HasValue)
        {
            query = query.Where(d => d.IsAvailable == isAvailable.Value);
        }
        
        var count = await query.CountAsync(cancellationToken);
        
        var desks = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    
        return (desks, count);
    }

    public void Update(Desk desk)
    {
        _dbContext.Desks.Update(desk);
    }
}