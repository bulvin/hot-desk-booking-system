using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public static class DbSeeder
{
    private static readonly Guid AdminRoleId = new("1e62f92c-d2e9-4169-ba84-24629a6c9f12");
    private static readonly Guid EmployeeRoleId = new("5ebfa14d-7c9c-4d3b-8f84-34c25c37215c");
    private static readonly Guid AdminUserId = new("c5d9e543-7c2f-4345-a014-ebd860eef718");

    public static async Task SeedData(AppDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            await SeedRoles(context);
        }

        if (!await context.Users.AnyAsync())
        {
            await SeedUsers(context);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedRoles(AppDbContext context)
    {
        var now = DateTime.UtcNow;

        var roles = new List<Role>
        {
            new() 
            { 
                Id = AdminRoleId,
                Name = UserRole.Administrator.ToString(),
                CreatedAt = now,
                UpdatedAt = now
            },
            new() 
            { 
                Id = EmployeeRoleId,
                Name = UserRole.Employee.ToString(),
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsers(AppDbContext context)
    {
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Id == AdminRoleId);
        if (adminRole == null) return;
        var now = DateTime.UtcNow;

        var adminUser = new User
        {
            Id = AdminUserId,
            Email = "a@c.pl",
            FirstName = "Admin",
            LastName = "User",
            Password = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            CreatedAt = now,
            UpdatedAt = now,
      
        };
        adminUser.Roles.Add(adminRole);
        await context.Users.AddAsync(adminUser);
    }
}