using Domain.Reservations;

namespace Application.Dtos;

public record DeskDetailsDto(
    Guid Id, 
    string Name, 
    string? Description, 
    Guid LocationId, 
    bool IsAvailable,
    ReservationWithUserDto Reservation
    );