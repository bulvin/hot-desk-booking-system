using Application.Desks.GetPagedByLocation;

namespace UnitTests.Desks;

public class GetDesksValidatorTests
{
    private readonly GetDesksValidator _validator = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_OmittedPagination_UsesDefaults(bool includeEmptyFilter)
    {
        var query = new GetDesksByLocationQuery(Guid.NewGuid(), DeskAvailabilityFilter: null, DateRange: null,
            includeEmptyFilter ? new PaginationFilter(Page: null, PageSize: null) : null);

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 30, "PaginationFilter.Page")]
    [InlineData(-1, 30, "PaginationFilter.Page")]
    [InlineData(1, 31, "PaginationFilter.PageSize")]
    public void Validate_InvalidPagination_ReturnsValidationError(int page, int pageSize, string propertyName)
    {
        var query = new GetDesksByLocationQuery(Guid.NewGuid(), DeskAvailabilityFilter: null, DateRange: null,
            new PaginationFilter(page, pageSize));

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, propertyName, StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MissingLocation_ReturnsValidationError()
    {
        var query = new GetDesksByLocationQuery(Guid.Empty, DeskAvailabilityFilter: null, DateRange: null, PaginationFilter: null);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "LocationId", StringComparison.Ordinal));
    }
}