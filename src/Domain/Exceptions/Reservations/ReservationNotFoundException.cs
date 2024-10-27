using System.Net;

namespace Domain.Exceptions.Reservations;

public class ReservationNotFoundException : HotDeskBookingException
{
    public Guid Id { get; }

    public ReservationNotFoundException(Guid reservationId) 
        : base($"Reservation with ID {reservationId} was not found")
    {
        Id = reservationId;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}