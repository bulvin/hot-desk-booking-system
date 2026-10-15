using Application.Interfaces;
using Application.Reservations.BookDesk;
using Moq;

namespace UnitTests.Reservations;

public class BookDeskCommandValidatorTests
{
    [Theory]
    [InlineData(-1, -1, false)]
    [InlineData(0, 0, true)]
    [InlineData(7, 7, true)]
    [InlineData(8, 8, false)]
    [InlineData(0, 6, true)]
    [InlineData(0, 7, false)]
    [InlineData(7, 13, true)]
    [InlineData(1, 0, false)]
    public void Validate_EnforcesInclusiveBookingBoundaries(int startOffset, int endOffset, bool valid)
    {
        var now = new DateTime(2026, 10, 15, 23, 59, 59, DateTimeKind.Utc);
        var clock = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        clock.SetupGet(provider => provider.UtcNow).Returns(now);
        var validator = new BookDeskCommandValidator(clock.Object);
        var today = DateOnly.FromDateTime(now);

        var result = validator.Validate(new BookDeskCommand(Guid.NewGuid(), today.AddDays(startOffset), today.AddDays(endOffset)));

        Assert.Equal(valid, result.IsValid);
        clock.VerifyGet(provider => provider.UtcNow, Times.Once);
    }

    [Fact]
    public async Task Validate_ReusedValidator_ObservesUtcMidnight()
    {
        var clock = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        clock.SetupSequence(provider => provider.UtcNow)
            .Returns(new DateTime(2026, 10, 15, 23, 59, 59, DateTimeKind.Utc))
            .Returns(new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc));
        var validator = new BookDeskCommandValidator(clock.Object);
        var date = new DateOnly(2026, 10, 15);
        var command = new BookDeskCommand(Guid.NewGuid(), date, date);

        Assert.True((await validator.ValidateAsync(command, TestContext.Current.CancellationToken)).IsValid);
        var result = await validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, nameof(command.StartDate), StringComparison.Ordinal));
        clock.VerifyGet(provider => provider.UtcNow, Times.Exactly(2));
    }
}
