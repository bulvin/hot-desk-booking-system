using Application.Interfaces;
using Domain.Desks;
using Domain.Locations;
using Domain.Users;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

internal static class SampleDataSeeder
{

#pragma warning disable S2068 // Intentionally public local demo password; never seeded outside Development.
    private const string SamplePassword = "LocalDemo123!";
#pragma warning restore S2068
    private static readonly (string Email, string FirstName, string Role)[] Accounts =
    [
        ("admin@hotdesk.example", "Admin", PolicyNames.Admin),
        ("employee@hotdesk.example", "Employee", PolicyNames.Employee)
    ];

    internal static void Seed(DbContext context, IDateTimeProvider clock)
    {
        if (!context.Set<Location>().Any())
            context.Add(CreateSampleLocation(clock.UtcNow));

        foreach (var account in Accounts)
        {
            var normalizedEmail = account.Email.ToUpperInvariant();
            if (context.Set<User>().Any(user => user.NormalizedEmail == normalizedEmail || user.NormalizedUserName == normalizedEmail))
                continue;

            var normalizedRole = account.Role.ToUpperInvariant();
            var role = context.Set<IdentityRole<Guid>>().SingleOrDefault(role => role.NormalizedName == normalizedRole);
            AddAccount(context, account.Email, account.FirstName, account.Role, role, clock.UtcNow);
        }
        context.SaveChanges();
    }

    internal static async Task SeedAsync(DbContext context, IDateTimeProvider clock, CancellationToken cancellationToken)
    {
        if (!await context.Set<Location>().AnyAsync(cancellationToken))
            context.Add(CreateSampleLocation(clock.UtcNow));

        foreach (var account in Accounts)
        {
            var normalizedEmail = account.Email.ToUpperInvariant();
            if (await context.Set<User>().AnyAsync(user => user.NormalizedEmail == normalizedEmail || user.NormalizedUserName == normalizedEmail, cancellationToken))
                continue;

            var normalizedRole = account.Role.ToUpperInvariant();
            var role = await context.Set<IdentityRole<Guid>>().SingleOrDefaultAsync(role => role.NormalizedName == normalizedRole, cancellationToken);
            AddAccount(context, account.Email, account.FirstName, account.Role, role, clock.UtcNow);
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void AddAccount(DbContext context, string email, string firstName, string roleName, IdentityRole<Guid>? role, DateTime now)
    {
        if (role is null)
        {
            role = new IdentityRole<Guid>(roleName)
            {
                Id = Guid.NewGuid(),
                NormalizedName = roleName.ToUpperInvariant()
            };
            context.Add(role);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FirstName = firstName,
            LastName = "Demo",
            SecurityStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, SamplePassword);
        context.Add(user);
        context.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
    }

    private static Location CreateSampleLocation(DateTime now)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Sample office",
            Address = new Address("Sample Street", "1", "Warsaw", "00-000"),
            CreatedAt = now,
            UpdatedAt = now
        };
        for (var number = 1; number <= 3; number++)
        {
            location.Desks.Add(new Desk
            {
                Id = Guid.NewGuid(),
                Name = $"D-{number:00}",
                Description = "Sample desk for local development",
                LocationId = location.Id,
                Location = location,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        return location;
    }
}
