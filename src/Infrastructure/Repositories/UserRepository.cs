using Domain.Users;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public void Add(User user)
    {
        dbContext.Add(user);
    }

    public async Task<bool> Exists(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<Role?> GetRoleByName(string name, CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<User?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(u => u.Roles)
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
}