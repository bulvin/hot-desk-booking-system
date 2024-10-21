namespace Domain.Reservations;

public interface IReservationRepository
{
    void Add(Reservation reservation);
    Task<bool> HasActiveReservationForDesk(Guid deskId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveReservationForDesk(Guid deskId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}