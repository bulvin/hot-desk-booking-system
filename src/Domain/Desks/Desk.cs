using Domain.Locations;
using Domain.Reservations;

namespace Domain.Desks;

public class Desk : Entity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsAvailable { get; set; }
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public ICollection<Reservation> Reservations = [];
}