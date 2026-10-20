using Domain.Reservations;

namespace Application.Dtos;

public record MyReservationDto(Guid Id, DateOnly StartDate, DateOnly EndDate, Status Status,
    Guid DeskId, string DeskName, Guid LocationId, string LocationName);
