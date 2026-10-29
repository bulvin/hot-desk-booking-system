using Application.Interfaces.CQRS;

namespace Application.Accounts.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<SessionDto>;
