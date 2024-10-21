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

    public async Task<bool> HasReservationsForDesk(Guid deskId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Reservations
            .AnyAsync(r => r.DeskId == deskId, cancellationToken);
    }
}