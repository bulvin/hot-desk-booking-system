namespace Application.Accounts;

public sealed record AccountDto(Guid Id, string Email, string FirstName, string LastName, bool IsActive, bool IsAdministrator);
