using Domain.Desks;
using Domain.Users;

namespace Domain.Reservations;

public class Reservation : Entity
{
    public Guid DeskId { get; set; }
    public Desk Desk { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}