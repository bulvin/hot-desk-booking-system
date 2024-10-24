using Domain.Desks;
using Domain.Users;

namespace Domain.Reservations;

public class Reservation : Entity
{
    public Guid DeskId { get; set; }
    public Desk Desk { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Status Status { get; set; } = Status.Reserved;
}

public enum Status
{
    Reserved,
    Completed,
    Canceled
}