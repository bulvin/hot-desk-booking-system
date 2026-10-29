using Application.Interfaces;
using Application.Interfaces.CQRS;

namespace Application.Accounts.SetEmployeeActive;

public sealed class SetEmployeeActiveHandler(IAccountService accounts) : ICommandHandler<SetEmployeeActiveCommand, bool>
{
    public async ValueTask<bool> Handle(SetEmployeeActiveCommand request, CancellationToken cancellationToken) =>
        await accounts.SetEmployeeActive(request, cancellationToken);
}
