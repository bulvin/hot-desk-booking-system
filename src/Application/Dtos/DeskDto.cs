using Domain.Reservations;

namespace Application.Dtos;

public record DeskDto(
    Guid Id, 
    string Name, 
    string? Description, 
    Guid LocationId, 
    bool IsAvailable);