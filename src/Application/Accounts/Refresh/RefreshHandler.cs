using System.Net;
using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain.Exceptions.Users;

namespace Application.Accounts.Refresh;

public sealed class RefreshHandler(IAccountService accounts) : ICommandHandler<RefreshCommand, SessionDto>
{
    public async ValueTask<SessionDto> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            throw new AccountException(HttpStatusCode.Unauthorized, "Session expired.");
        return await accounts.Refresh(request, cancellationToken);
    }
}
