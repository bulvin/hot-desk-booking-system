using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Desks.GetPagedByLocation;

public record GetDesksByLocationQuery(
    Guid LocationId,
    DeskAvailabilityFilter? DeskAvailabilityFilter,
    DateRange? DateRange,
    PaginationFilter? PaginationFilter) : IQuery<PagedDto<DeskDto>>;

public record GetDesksByLocationRequest(
    bool? IsAvailable,
    bool? IsBookable,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Page,
    int? PageSize);

public record DateRange(DateOnly? StartDate, DateOnly? EndDate);

public record PaginationFilter(int? Page, int? PageSize);

public record DeskAvailabilityFilter(bool? IsAvailable, bool? IsBookable);
