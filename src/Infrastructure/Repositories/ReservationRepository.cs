using Domain.Reservations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReservationRepository(AppDbContext dbContext) : IReservationRepository
{
    public void Add(Reservation reservation)
    {
        dbContext.Reservations.Add(reservation);
    }

    public void Update(Reservation reservation)
    {
        dbContext.Reservations.Update(reservation);
    }

    public async Task<bool> HasActiveReservationForDesk(Guid deskId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Reservations
            .AnyAsync(r => r.DeskId == deskId && r.Status == Status.Reserved, cancellationToken);
    }

    public async Task<bool> HasActiveReservationForDesk(Guid deskId, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Reservations
            .AnyAsync(r =>
                    r.DeskId == deskId &&
                    r.Status == Status.Reserved &&
                    r.StartDate <= endDate &&
                    r.EndDate >= startDate,
                cancellationToken);
    }

    public async Task<Reservation?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken: cancellationToken);
    }

    public async Task<Reservation?> GetByDesk(Guid deskId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Reservations
            .Include(r => r.Desk)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.DeskId == deskId && r.Status == Status.Reserved, cancellationToken: cancellationToken);
    }
}