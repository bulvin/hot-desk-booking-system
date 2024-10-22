namespace Domain.Users;

public interface IUserRepository
{
    void Add(User user);
    Task<bool> Exists(string email, CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByName(string name, CancellationToken cancellationToken = default);
}