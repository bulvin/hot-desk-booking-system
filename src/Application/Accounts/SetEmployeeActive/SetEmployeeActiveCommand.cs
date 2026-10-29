using Application.Interfaces.CQRS;

namespace Application.Accounts.SetEmployeeActive;

public sealed record SetEmployeeActiveCommand(Guid Id, bool IsActive) : ICommand<bool>;
