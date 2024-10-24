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
        return await _dbContext.Desks
            .Include(d => d.Reservations.Where(r => r.Status == Status.Reserved)) 
            .ThenInclude(r => r.User) 
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<(List<Desk> Desks, int Count)> GetDesksByLocation(Guid locationId, bool? isAvailable, bool? isBookable, DateOnly startDate, DateOnly endDate, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Desks
            .Include(d => d.Reservations.Where(r => r.Status == Status.Reserved))
            .Where(d => d.LocationId == locationId); 
  
        if (isAvailable.HasValue)
        {
            query = query.Where(d => d.IsAvailable == isAvailable.Value);
        }

        if (isBookable.HasValue)
        {
            if (isBookable.Value)
            {
                query = query.Where(d => !d.Reservations.Any(r =>
                    r.StartDate <= endDate &&
                    r.EndDate >= startDate
                ));
            }
            else
            {
                query = query.Where(d => d.Reservations.Any(r => 
                    r.StartDate <= endDate &&
                    r.EndDate >= startDate));
            }
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