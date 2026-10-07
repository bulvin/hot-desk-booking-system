using Domain.Reservations;

namespace Application.Dtos;

public record ReservationWithUserDto(Guid Id, DateOnly StartDate, DateOnly EndDate, Status Status, UserReservesDto? User);