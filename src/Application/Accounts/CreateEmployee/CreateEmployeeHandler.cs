using Application.Interfaces;
using Application.Interfaces.CQRS;

namespace Application.Accounts.CreateEmployee;

public sealed class CreateEmployeeHandler(IAccountService accounts) : ICommandHandler<CreateEmployeeCommand, AccountDto>
{
    public async ValueTask<AccountDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken) =>
        await accounts.CreateEmployee(request, cancellationToken);
}
