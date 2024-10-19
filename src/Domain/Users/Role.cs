namespace Domain.Users;

public class Role : Entity
{
    public string Name { get; set; } = default!;
    public ICollection<User> Users { get; set; } = new List<User>();
}