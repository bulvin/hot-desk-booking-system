using Domain.Desks;
using Domain.Locations;
using Domain.Users;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Data;

public class AppDbContextTests
{
    [Fact]
    public void Model_CollectionNavigationChanges_DoNotRequireMigration()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unit_tests;Username=unit_tests")
            .Options;
        using var context = new AppDbContext(options);

        Assert.False(context.Database.HasPendingModelChanges());
        Assert.NotNull(context.Model.FindEntityType(typeof(Desk))?.FindNavigation(nameof(Desk.Reservations)));
        Assert.NotNull(context.Model.FindEntityType(typeof(User))?.FindSkipNavigation(nameof(User.Roles)));
        Assert.NotNull(context.Model.FindEntityType(typeof(User))?.FindNavigation(nameof(User.Reservations)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Role))?.FindSkipNavigation(nameof(Role.Users)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Location))?.FindNavigation(nameof(Location.Desks)));
    }
}