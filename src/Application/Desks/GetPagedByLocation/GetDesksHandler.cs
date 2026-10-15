using Application.Dtos;
using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain.Desks;

namespace Application.Desks.GetPagedByLocation;

public class GetDesksHandler(IDeskRepository repository, IDateTimeProvider dateTimeProvider) : IQueryHandler<GetDesksByLocationQuery, PagedDto<DeskDto>>
{
    public async ValueTask<PagedDto<DeskDto>> Handle(GetDesksByLocationQuery query, CancellationToken cancellationToken)
    {
        var page = query.PaginationFilter?.Page ?? 1;
        var pageSize = query.PaginationFilter?.PageSize ?? 30;
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        var startDate = query.DateRange?.StartDate ?? today;
        var endDate = query.DateRange?.EndDate ?? today;

        var (desks, totalCount) = await repository.GetDesksByLocation(
            query.LocationId,
            query.DeskAvailabilityFilter?.IsAvailable,
            query.DeskAvailabilityFilter?.IsBookable,
            startDate,
            endDate,
            page,
            pageSize,
            cancellationToken);

        var desksDtos = desks.Select(desk => new DeskDto(
            desk.Id, desk.Name, desk.Description, desk.LocationId, desk.IsAvailable)).ToList();
        return new PagedDto<DeskDto>(desksDtos, page, pageSize, totalCount);
    }
}
