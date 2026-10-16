using Domain.Reservations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReservationRepository(AppDbContext dbContext) : IReservationRepository
{
    private readonly DbSet<Reservation> _reservations = dbContext.Reservations;

    public Task<bool> HasReservationForDesk(Guid deskId, CancellationToken cancellationToken = default)
    {
        return ReservationsForDesk(deskId).AnyAsync(cancellationToken);
    }

    public void Add(Reservation reservation)
    {
        _reservations.Add(reservation);
    }

    public void Update(Reservation reservation)
    {
        _reservations.Update(reservation);
    }

    public Task<bool> HasActiveReservationForDesk(Guid deskId, CancellationToken cancellationToken = default)
    {
        return ReservationsForDesk(deskId, Status.Reserved).AnyAsync(cancellationToken);
    }

    public Task<bool> HasActiveReservationForDesk(Guid deskId, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return ReservationsForDesk(deskId, Status.Reserved)
            .AnyAsync(r => r.StartDate <= endDate && r.EndDate >= startDate, cancellationToken);
    }

    public Task<Reservation?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return _reservations
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken: cancellationToken);
    }

    public Task<Reservation?> GetByDesk(Guid deskId, CancellationToken cancellationToken = default)
    {
        return ReservationsForDesk(deskId, Status.Reserved)
            .Include(r => r.Desk)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Reservation> ReservationsForDesk(Guid deskId, Status? status = null)
    {
        var query = _reservations.Where(r => r.DeskId == deskId);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return query;
    }
}
