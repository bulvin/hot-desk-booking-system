using System.Net;

namespace Domain.Exceptions.Desks;

public class DeskAlreadyReservedException : HotDeskBookingException
{
    public Guid Id { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    public DeskAlreadyReservedException(Guid deskId, DateOnly startDate, DateOnly endDate) 
        : base($"Desk {deskId} is already reserved for period from {startDate:d} to {endDate:d}")
    {
        Id = deskId;
        StartDate = startDate;
        EndDate = endDate;
    }

    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}