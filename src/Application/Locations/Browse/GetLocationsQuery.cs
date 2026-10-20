using Application.Browsing;
using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Locations.Browse;

public record GetLocationsQuery(PageRequest Pagination) : IQuery<PagedDto<LocationDto>>;
