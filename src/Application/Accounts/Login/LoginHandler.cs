using Application.Interfaces;
using Application.Interfaces.CQRS;

namespace Application.Accounts.Login;

public sealed class LoginHandler(IAccountService accounts) : ICommandHandler<LoginCommand, SessionDto>
{
    public async ValueTask<SessionDto> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        await accounts.Login(request, cancellationToken);
}
