using Application.Interfaces;
using Domain.Desks;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

internal static class SampleDataSeeder
{
    internal static void Seed(DbContext context, IDateTimeProvider clock)
    {
        if (context.Set<Location>().Any())
            return;

        context.Add(CreateSampleLocation(clock.UtcNow));
        context.SaveChanges();
    }

    internal static async Task SeedAsync(DbContext context, IDateTimeProvider clock, CancellationToken cancellationToken)
    {
        if (await context.Set<Location>().AnyAsync(cancellationToken))
            return;

        context.Add(CreateSampleLocation(clock.UtcNow));
        await context.SaveChangesAsync(cancellationToken);
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
