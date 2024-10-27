using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain.Desks;
using Domain.Exceptions;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Locations;
using Domain.Reservations;
using Domain.Users;
using Microsoft.AspNetCore.Http;

namespace Application.Desks.Get;

public record GetDeskDetailsQuery(Guid Id, Guid LocationId) : IQuery<DeskDetailsDto>;

public class GetDeskHandler : IQueryHandler<GetDeskDetailsQuery, DeskDetailsDto>
{
    private readonly IDeskRepository _deskRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDeskHandler(IHttpContextAccessor httpContextAccessor, IDeskRepository deskRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _deskRepository = deskRepository;
    }
    
    public async Task<DeskDetailsDto> Handle(GetDeskDetailsQuery query, CancellationToken cancellationToken)
    {
        var desk = await _deskRepository.GetById(query.Id, cancellationToken)
                                 ?? throw new DeskNotFoundException(query.Id);

        if (desk.LocationId != query.LocationId)
            throw new LocationNotFoundException(query.LocationId);

        var activeReservation =  desk.Reservations.FirstOrDefault();
        ReservationWithUserDto? reservation = null;
        if (activeReservation is not null)
            reservation = CreateReservationDto(activeReservation);

        return new DeskDetailsDto(
            desk.Id,
            desk.Name,
            desk.Description,
            desk.LocationId,
            desk.IsAvailable,
            reservation);
    }

    private ReservationWithUserDto? CreateReservationDto(Reservation reservation)
    {
        var isAdmin = _httpContextAccessor.HasRole(UserRole.Administrator);
        
        UserReservesDto? userDto = null;
        if (!isAdmin)
            return new ReservationWithUserDto(
                reservation.Id,
                reservation.StartDate,
                reservation.EndDate,
                reservation.Status,
                userDto);
        
        var fullName = $"{reservation.User.FirstName} {reservation.User.LastName}";
        userDto = new UserReservesDto(reservation.UserId, fullName);

        return new ReservationWithUserDto(
            reservation.Id,
            reservation.StartDate,
            reservation.EndDate,
            reservation.Status,
            userDto);
    }
}