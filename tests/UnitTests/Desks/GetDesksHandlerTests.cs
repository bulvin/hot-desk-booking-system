using Application.Desks.GetPagedByLocation;
using Application.Interfaces;
using Domain.Desks;
using Moq;

namespace UnitTests.Desks;

public class GetDesksHandlerTests
{
    [Theory]
    [InlineData(15, 23, 59, 59, false, false)]
    [InlineData(16, 0, 0, 0, false, false)]
    [InlineData(16, 0, 0, 0, true, false)]
    [InlineData(16, 0, 0, 0, false, true)]
    [InlineData(16, 0, 0, 0, true, true)]
    public async Task Handle_UsesUtcDefaultsAndPreservesSuppliedDates(int day, int hour, int minute, int second, bool supplyStart, bool supplyEnd)
    {
        var clock = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        clock.SetupGet(provider => provider.UtcNow).Returns(new DateTime(2026, 10, day, hour, minute, second, DateTimeKind.Utc));
        var repository = new Mock<IDeskRepository>(MockBehavior.Strict);
        var today = new DateOnly(2026, 10, day);
        var start = supplyStart ? today.AddDays(1) : today;
        var end = supplyEnd ? today.AddDays(2) : today;
        var query = new GetDesksByLocationQuery(Guid.NewGuid(), null,
            new DateRange(supplyStart ? start : null, supplyEnd ? end : null), null);
        repository.Setup(repo => repo.GetDesksByLocation(query.LocationId, null, null, start, end, 1, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Desk>(), 0));

        await new GetDesksHandler(repository.Object, clock.Object).Handle(query, TestContext.Current.CancellationToken);

        repository.VerifyAll();
        clock.VerifyGet(provider => provider.UtcNow, Times.Once);
    }

    [Fact]
    public async Task Handle_AbsentDateRange_UsesOneClockReadingAcrossMidnight()
    {
        var clock = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        clock.SetupSequence(provider => provider.UtcNow)
            .Returns(new DateTime(2026, 10, 15, 23, 59, 59, DateTimeKind.Utc))
            .Returns(new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc));
        var repository = new Mock<IDeskRepository>(MockBehavior.Strict);
        var query = new GetDesksByLocationQuery(Guid.NewGuid(), null, null, null);
        var today = new DateOnly(2026, 10, 15);
        repository.Setup(repo => repo.GetDesksByLocation(query.LocationId, null, null, today, today, 1, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Desk>(), 0));

        await new GetDesksHandler(repository.Object, clock.Object).Handle(query, TestContext.Current.CancellationToken);

        repository.VerifyAll();
        clock.VerifyGet(provider => provider.UtcNow, Times.Once);
    }
}
