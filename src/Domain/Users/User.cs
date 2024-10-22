using Domain.Reservations;

namespace Domain.Users;

public class User : Entity
{
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
}

public enum UserRole
{
    Employee,
    Administrator
}