using System.Net;

namespace Domain.Exceptions.Reservations;

public class TooLateForReservationChangeException(Guid reservationId, DateOnly reservationDate)
    : HotDeskBookingException(
        $"Cannot change reservation {reservationId} as it starts in less than 24 hours (on {reservationDate})")
{
    public Guid ReservationId { get; } = reservationId;
    public DateOnly ReservationDate { get; } = reservationDate;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}