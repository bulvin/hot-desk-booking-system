using Application.Interfaces.CQRS;

namespace Application.Accounts.Refresh;

public sealed record RefreshCommand(string RefreshToken) : ICommand<SessionDto>;
