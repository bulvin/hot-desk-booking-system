using Application.Dtos;
using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain.Reservations;

namespace Application.Reservations.Browse;

public sealed class GetMyReservationsHandler(IReservationRepository repository, IUserContext userContext)
    : IQueryHandler<GetMyReservationsQuery, PagedDto<MyReservationDto>>
{
    public async ValueTask<PagedDto<MyReservationDto>> Handle(GetMyReservationsQuery query, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetPageForUser(userContext.GetCurrentUser().Id,
            query.Pagination.Page, query.Pagination.PageSize, cancellationToken);
        return new PagedDto<MyReservationDto>(items.Select(x => new MyReservationDto(x.Id, x.StartDate,
            x.EndDate, x.Status, x.DeskId, x.Desk.Name, x.Desk.LocationId, x.Desk.Location.Name)).ToList(),
            query.Pagination.Page, query.Pagination.PageSize, total);
    }
}
