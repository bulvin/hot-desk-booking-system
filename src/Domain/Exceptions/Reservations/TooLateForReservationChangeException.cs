using System.Net;

namespace Domain.Exceptions.Reservations;

public class TooLateForReservationChangeException(Guid reservationId, DateOnly reservationDate)
    : HotDeskBookingException(
        $"Cannot change reservation {reservationId} on or after its start date ({reservationDate}) in the UTC calendar")
{
    public Guid ReservationId { get; } = reservationId;
    public DateOnly ReservationDate { get; } = reservationDate;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}
