using System.Net;

namespace Domain.Exceptions.Reservations;

public class ReservationNotFoundException(Guid reservationId)
    : HotDeskBookingException($"Reservation with ID {reservationId} was not found")
{
    public Guid Id { get; } = reservationId;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}