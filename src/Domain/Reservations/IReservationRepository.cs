namespace Domain.Reservations;

public interface IReservationRepository
{
    Task<bool> HasReservationsForDesk(Guid deskId, CancellationToken cancellationToken = default);
}