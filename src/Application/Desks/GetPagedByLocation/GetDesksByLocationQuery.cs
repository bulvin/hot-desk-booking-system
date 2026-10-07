using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Desks.GetPagedByLocation;

public record GetDesksByLocationQuery(
    Guid LocationId,
    DeskAvailabilityFilter? DeskAvailabilityFilter,
    DateRange? DateRange,
    PaginationFilter? PaginationFilter) : IQuery<PagedDto<DeskDto>>;