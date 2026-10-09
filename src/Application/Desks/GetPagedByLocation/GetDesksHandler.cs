using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain.Desks;

namespace Application.Desks.GetPagedByLocation;

public class GetDesksHandler(IDeskRepository repository) : IQueryHandler<GetDesksByLocationQuery, PagedDto<DeskDto>>
{
    public async ValueTask<PagedDto<DeskDto>> Handle(GetDesksByLocationQuery query, CancellationToken cancellationToken)
    {
        var page = query.PaginationFilter?.Page ?? 1;
        var pageSize = query.PaginationFilter?.PageSize ?? 30;
        var startDate = query.DateRange?.StartDate ?? DateOnly.FromDateTime(DateTime.Today);
        var endDate = query.DateRange?.EndDate ?? DateOnly.FromDateTime(DateTime.Today);

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