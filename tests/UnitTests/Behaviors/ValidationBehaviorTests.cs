using Application.Behaviors;
using Application.Desks.GetPagedByLocation;

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

        var result = await behavior.Handle(request, (_, token) =>
        {
            receivedToken = token;
            return ValueTask.FromResult(42);
        }, cancellation.Token);

        Assert.Equal(42, result);
        Assert.Equal(cancellation.Token, receivedToken);
    }
}
