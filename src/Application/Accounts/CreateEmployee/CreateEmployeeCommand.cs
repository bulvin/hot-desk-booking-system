using Application.Interfaces.CQRS;

namespace Application.Accounts.CreateEmployee;

public sealed record CreateEmployeeCommand(string Email, string FirstName, string LastName, string Password) : ICommand<AccountDto>;
