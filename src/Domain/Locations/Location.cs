using Domain.Desks;

namespace Domain.Locations;

public class Location : Entity
{
    public string Name { get; set; } = default!;
    public Address Address { get; set; } = null!;
    public ICollection<Desk> Desks { get; set; } = [];
}