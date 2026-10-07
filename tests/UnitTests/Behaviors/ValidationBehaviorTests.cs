using Application.Behaviors;
using Application.Desks.GetPagedByLocation;
using FluentValidation;

namespace UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_ForwardsCancellationToNext(bool includeValidator)
    {
        var validator = new GetDesksValidator();
        var behavior = new ValidationBehavior<GetDesksByLocationQuery, int>(
            includeValidator ? [validator] : []);
        var request = new GetDesksByLocationQuery(Guid.NewGuid(), DeskAvailabilityFilter: null, DateRange: null, PaginationFilter: null);
        using var cancellation = new CancellationTokenSource();
        var receivedToken = CancellationToken.None;

        var result = await behavior.Handle(request, token =>
        {
            receivedToken = token;
            return Task.FromResult(42);
        }, cancellation.Token);

        Assert.Equal(42, result);
        Assert.Equal(cancellation.Token, receivedToken);
    }

    [Fact]
    public async Task Handle_InvalidRequest_DoesNotInvokeNext()
    {
        var behavior = new ValidationBehavior<GetDesksByLocationQuery, int>([new GetDesksValidator()]);
        var request = new GetDesksByLocationQuery(Guid.Empty, DeskAvailabilityFilter: null, DateRange: null, PaginationFilter: null);
        var nextCalled = false;

        await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(request, _ =>
        {
            nextCalled = true;
            return Task.FromResult(42);
        }, CancellationToken.None));

        Assert.False(nextCalled);
    }
}