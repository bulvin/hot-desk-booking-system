using System.Net;

namespace Domain.Exceptions.Reservations;

public class UnauthorizedReservationChangeException(Guid reservationId, Guid userId)
    : HotDeskBookingException($"User {userId} is not authorized to change reservation {reservationId}")
{
    public Guid ReservationId { get; } = reservationId;
    public Guid UserId { get; } = userId;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Forbidden;
}