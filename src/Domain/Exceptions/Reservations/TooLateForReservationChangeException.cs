using System.Net;

namespace Domain.Exceptions.Reservations;

public class TooLateForReservationChangeException : HotDeskBookingException
{
    public Guid ReservationId { get; }
    public DateOnly ReservationDate { get; }

    public TooLateForReservationChangeException(Guid reservationId, DateOnly reservationDate) 
        : base($"Cannot change reservation {reservationId} as it starts in less than 24 hours (on {reservationDate})")
    {
        ReservationId = reservationId;
        ReservationDate = reservationDate;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}