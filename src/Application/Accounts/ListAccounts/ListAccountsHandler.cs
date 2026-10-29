using Application.Dtos;
using Application.Interfaces;
using Application.Interfaces.CQRS;

namespace Application.Accounts.ListAccounts;

public sealed class ListAccountsHandler(IAccountService accounts) : IQueryHandler<ListAccountsQuery, PagedDto<AccountDto>>
{
    public async ValueTask<PagedDto<AccountDto>> Handle(ListAccountsQuery request, CancellationToken cancellationToken) =>
        await accounts.ListAccounts(request, cancellationToken);
}
