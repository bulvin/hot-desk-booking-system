using Domain.Reservations;

namespace Application.Dtos;

public record ReservationDto(Guid Id, DateOnly StartDate, DateOnly EndDate, Status Status);

public record ReservationWithUserDto(Guid Id, DateOnly StartDate, DateOnly EndDate, Status Status, UserReservesDto User);