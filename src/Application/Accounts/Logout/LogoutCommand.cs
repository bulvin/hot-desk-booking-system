using Application.Interfaces.CQRS;

namespace Application.Accounts.Logout;

public sealed record LogoutCommand(string? RefreshToken) : ICommand<bool>;
