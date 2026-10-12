namespace Application.Dtos;

public sealed record ProfileDto(Guid Id, string Email, string FirstName, string LastName,
    IReadOnlyCollection<string> Roles, bool EmailConfirmed);
