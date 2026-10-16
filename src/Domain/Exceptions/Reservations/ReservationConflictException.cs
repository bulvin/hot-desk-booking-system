using System.Net;

namespace Domain.Exceptions.Reservations;

public class ReservationConflictException(Guid reservationId)
    : HotDeskBookingException($"Reservation {reservationId} cannot be modified in its current state")
{
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}
