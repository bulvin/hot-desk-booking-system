using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Accounts.ListAccounts;

public sealed record ListAccountsQuery(int Page = 1, int PageSize = 20, string? Search = null, bool? IsActive = null) : IQuery<PagedDto<AccountDto>>;
