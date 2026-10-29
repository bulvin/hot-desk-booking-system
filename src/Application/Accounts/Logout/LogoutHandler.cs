using Application.Interfaces;
using Application.Interfaces.CQRS;

namespace Application.Accounts.Logout;

public sealed class LogoutHandler(IAccountService accounts) : ICommandHandler<LogoutCommand, bool>
{
    public async ValueTask<bool> Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        await accounts.Logout(request, cancellationToken);
}
