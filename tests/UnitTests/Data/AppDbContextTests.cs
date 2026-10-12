using Domain.Desks;
using Domain.Locations;
using Domain.Reservations;
using Domain.Users;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Data;

public class AppDbContextTests
{
    [Fact]
    public void Model_BookingRelationships_MatchMigration()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unit_tests;Username=unit_tests")
            .Options;
        using var context = new AppDbContext(options);

        Assert.False(context.Database.HasPendingModelChanges());
        Assert.NotNull(context.Model.FindEntityType(typeof(Desk))?.FindNavigation(nameof(Desk.Reservations)));
        var reservation = context.Model.FindEntityType(typeof(Reservation));
        Assert.NotNull(reservation);
        Assert.NotNull(reservation.FindProperty(nameof(Reservation.UserId)));
        var userForeignKey = Assert.Single(reservation.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(User));
        Assert.Equal(nameof(Reservation.UserId), Assert.Single(userForeignKey.Properties).Name);
        Assert.Equal(DeleteBehavior.NoAction, userForeignKey.DeleteBehavior);
        Assert.NotNull(reservation.FindNavigation(nameof(Reservation.User)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Location))?.FindNavigation(nameof(Location.Desks)));
    }
}
