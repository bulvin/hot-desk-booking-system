using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskAlreadyReservedException(Guid deskId, DateOnly startDate, DateOnly endDate)
    : HotDeskBookingException($"Desk {deskId} is already reserved for period from {startDate:d} to {endDate:d}")
{
    public Guid Id { get; } = deskId;
    public DateOnly StartDate { get; } = startDate;
    public DateOnly EndDate { get; } = endDate;

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}