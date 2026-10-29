using Application.Accounts;
using Application.Accounts.CreateEmployee;
using Application.Accounts.ListAccounts;
using Application.Accounts.Login;
using Application.Accounts.Logout;
using Application.Accounts.Refresh;
using Application.Accounts.SetEmployeeActive;
using Application.Dtos;

namespace Application.Interfaces;

public interface IAccountService
{
    Task<SessionDto> Login(LoginCommand request, CancellationToken cancellationToken);
    Task<SessionDto> Refresh(RefreshCommand request, CancellationToken cancellationToken);
    Task<bool> Logout(LogoutCommand request, CancellationToken cancellationToken);
    Task<AccountDto> CreateEmployee(CreateEmployeeCommand request, CancellationToken cancellationToken);
    Task<bool> SetEmployeeActive(SetEmployeeActiveCommand request, CancellationToken cancellationToken);
    Task<PagedDto<AccountDto>> ListAccounts(ListAccountsQuery request, CancellationToken cancellationToken);
}
