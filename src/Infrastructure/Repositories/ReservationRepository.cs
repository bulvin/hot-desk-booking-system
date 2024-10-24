using Domain.Reservations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _dbContext;

    public ReservationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Reservation reservation)
    {
        _dbContext.Reservations.Add(reservation);
    }

    public void Update(Reservation reservation)
    {
        _dbContext.Reservations.Update(reservation);
    }

    public async Task<bool> HasActiveReservationForDesk(Guid deskId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Reservations
            .AnyAsync(r => r.DeskId == deskId && r.Status == Status.Reserved, cancellationToken);
    }

    public async Task<bool> HasActiveReservationForDesk(Guid deskId, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Reservations
            .AnyAsync(r =>
                    r.DeskId == deskId &&
                    r.Status == Status.Reserved &&
                    r.StartDate <= endDate &&
                    r.EndDate >= startDate,
                cancellationToken);
    }
    
    public async Task<Reservation?> GetById(Guid id,  CancellationToken cancellationToken = default)
    {
        return await _dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken: cancellationToken);
    }

    public async Task<Reservation?> GetByDesk(Guid deskId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Reservations
            .Include(r => r.Desk)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.DeskId == deskId && r.Status == Status.Reserved, cancellationToken: cancellationToken);
    }
}