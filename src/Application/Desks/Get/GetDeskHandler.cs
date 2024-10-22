using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Reservations;

namespace Application.Desks.Get;

public record GetDeskDetailsQuery(Guid Id, Guid LocationId) : IQuery<DeskDetailsDto>;

public class GetDeskHandler : IQueryHandler<GetDeskDetailsQuery, DeskDetailsDto>
{
    private readonly IReservationRepository _reservationRepository;

    public GetDeskHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<DeskDetailsDto> Handle(GetDeskDetailsQuery query, CancellationToken cancellationToken)
    {
        var reservationForDesk = await _reservationRepository.GetByDesk(query.Id, cancellationToken)
                   ?? throw new ApplicationException("desk not found");

        if (reservationForDesk.Desk.LocationId != query.LocationId)
            throw new ApplicationException("location not found");
        
        var fullName = reservationForDesk.User.FirstName + " " + reservationForDesk.User.LastName;
        var deskDto = new DeskDetailsDto(
            reservationForDesk.DeskId,
            reservationForDesk.Desk.Name,
            reservationForDesk.Desk.Description,
            reservationForDesk.Desk.LocationId,
            reservationForDesk.Desk.IsAvailable,
            new ReservationWithUserDto(reservationForDesk.Id, reservationForDesk.StartDate, reservationForDesk.EndDate, reservationForDesk.Status, 
                new UserReservesDto(reservationForDesk.UserId, fullName)));

        return deskDto;
    }
}