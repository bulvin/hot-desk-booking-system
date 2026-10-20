using Application.Browsing;
using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Reservations.Browse;

public record GetMyReservationsQuery(PageRequest Pagination) : IQuery<PagedDto<MyReservationDto>>;
