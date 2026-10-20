using Application.Dtos;
using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain.Desks;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Locations;
using Domain.Reservations;
using Domain.Users;

namespace Application.Desks.GetDetails;

public class GetDeskHandler(IUserContext userContext, IDeskRepository deskRepository, IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDeskDetailsQuery, DeskDetailsDto>
{
    public async ValueTask<DeskDetailsDto> Handle(GetDeskDetailsQuery query, CancellationToken cancellationToken)
    {
        var desk = await deskRepository.GetById(query.Id, cancellationToken)
                                 ?? throw new DeskNotFoundException(query.Id);

        if (desk.LocationId != query.LocationId)
            throw new LocationNotFoundException(query.LocationId);

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        var activeReservation = desk.Reservations
            .Where(r => r.Status == Status.Reserved && r.EndDate >= today)
            .OrderBy(r => r.StartDate)
            .ThenBy(r => r.Id)
            .FirstOrDefault();
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

    private ReservationWithUserDto CreateReservationDto(Reservation reservation)
    {
        var isAdmin = userContext.GetCurrentUser().Roles.Contains(nameof(UserRole.Administrator), StringComparer.Ordinal);

        UserReservesDto? userDto = null;
        if (!isAdmin)
            return new ReservationWithUserDto(
                reservation.Id,
                reservation.StartDate,
                reservation.EndDate,
                reservation.Status,
                userDto);

        var name = string.Join(' ', new[] { reservation.User.FirstName, reservation.User.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        userDto = new UserReservesDto(reservation.UserId, string.IsNullOrWhiteSpace(name) ? null : name);

        return new ReservationWithUserDto(
            reservation.Id,
            reservation.StartDate,
            reservation.EndDate,
            reservation.Status,
            userDto);
    }
}
