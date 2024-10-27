using System.Net;

namespace Domain.Exceptions.Reservations;

public class UnauthorizedReservationChangeException : HotDeskBookingException
{
    public Guid ReservationId { get; }
    public Guid UserId { get; }

    public UnauthorizedReservationChangeException(Guid reservationId, Guid userId) 
        : base($"User {userId} is not authorized to change reservation {reservationId}")
    {
        ReservationId = reservationId;
        UserId = userId;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Forbidden;
}