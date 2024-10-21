namespace Domain.Reservations;

public interface IReservationRepository
{
    Task<bool> HasActiveReservationForDesk(Guid deskId, CancellationToken cancellationToken = default);
}