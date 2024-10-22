using Domain.Users;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(User user)
    {
        _dbContext.Add(user);
    }
    
    public async Task<bool> Exists(string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<Role?> GetRoleByName(string name, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }
}