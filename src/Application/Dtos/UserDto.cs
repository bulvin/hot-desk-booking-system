namespace Application.Dtos;

public record UserDto(Guid Id, string Email, string FirstName, string LastName);

public record UserReservesDto(Guid Id, string Name);