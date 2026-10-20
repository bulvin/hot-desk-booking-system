using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain.Locations;

namespace Application.Locations.Browse;

public sealed class GetLocationsHandler(ILocationRepository repository) : IQueryHandler<GetLocationsQuery, PagedDto<LocationDto>>
{
    public async ValueTask<PagedDto<LocationDto>> Handle(GetLocationsQuery query, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetPage(query.Pagination.Page, query.Pagination.PageSize, cancellationToken);
        return new PagedDto<LocationDto>(items.Select(x => new LocationDto(x.Id, x.Name,
            new AddressDto(x.Address.Street, x.Address.BuildingNumber, x.Address.City, x.Address.PostalCode))).ToList(),
            query.Pagination.Page, query.Pagination.PageSize, total);
    }
}
