using System.Net;
using System.Security.Cryptography;
using System.Text;
using Application.Accounts;
using Application.Accounts.CreateEmployee;
using Application.Accounts.ListAccounts;
using Application.Accounts.Login;
using Application.Accounts.Logout;
using Application.Accounts.Refresh;
using Application.Accounts.SetEmployeeActive;
using Application.Dtos;
using Application.Interfaces;
using Domain;
using Domain.Exceptions.Users;
using Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Authentication;

internal sealed class AccountService(AppDbContext db, UserManager<User> users, RoleManager<IdentityRole<Guid>> roles,
    JwtIssuer issuer, IOptions<JwtOptions> options, IDateTimeProvider clock, IUnitOfWork unitOfWork) : IAccountService
{
    public async Task<SessionDto> Login(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.ExecuteInTransaction<SessionDto?>(async token =>
        {
            var normalizedEmail = users.NormalizeEmail(request.Email.Trim());
            var account = await db.Users.FromSqlInterpolated($"""SELECT * FROM "HotDeskBooking"."AspNetUsers" WHERE "NormalizedEmail" = {normalizedEmail} FOR UPDATE""")
                .SingleOrDefaultAsync(token);
            if (account is null || !account.IsActive || await users.IsLockedOutAsync(account))
                throw Unauthorized();
            if (!await users.CheckPasswordAsync(account, request.Password))
            {
                Check(await users.AccessFailedAsync(account));
                return null;
            }
            Check(await users.ResetAccessFailedCountAsync(account));
            var session = new AuthSession { UserId = account.Id, ExpiresAt = clock.UtcNow.AddDays(options.Value.SessionDays) };
            var refresh = NewRefreshToken();
            db.AuthSessions.Add(session);
            db.RefreshCredentials.Add(new RefreshCredential { Hash = Hash(refresh), SessionId = session.Id });
            return await issuer.Issue(account, session, refresh, token);
        }, cancellationToken);

        return result ?? throw Unauthorized();
    }

    public async Task<SessionDto> Refresh(RefreshCommand request, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.ExecuteInTransaction<SessionDto?>(async token =>
        {
            var hash = Hash(request.RefreshToken);
            var credential = await db.RefreshCredentials.AsNoTracking().SingleOrDefaultAsync(x => x.Hash == hash, token) ?? throw Unauthorized();
            var session = await LockSession(credential.SessionId, token);
            // Reload after acquiring the family lock: concurrent requests cannot both consume a token.
            credential = await db.RefreshCredentials.SingleAsync(x => x.Hash == hash, token);
            if (credential.IsUsed)
            {
                session.IsRevoked = true;
                return null;
            }
            var account = await db.Users.SingleAsync(x => x.Id == session.UserId, token);
            if (session.IsRevoked || session.ExpiresAt <= clock.UtcNow || !account.IsActive)
                throw Unauthorized();
            credential.IsUsed = true;
            var refresh = NewRefreshToken();
            db.RefreshCredentials.Add(new RefreshCredential { Hash = Hash(refresh), SessionId = session.Id });
            return await issuer.Issue(account, session, refresh, token);
        }, cancellationToken);

        return result ?? throw Unauthorized();
    }

    public async Task<bool> Logout(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return true;
        var hash = Hash(request.RefreshToken);
        var credential = await db.RefreshCredentials.AsNoTracking().SingleOrDefaultAsync(x => x.Hash == hash, cancellationToken);
        if (credential is not null)
        {
            var session = await db.AuthSessions.SingleAsync(x => x.Id == credential.SessionId, cancellationToken);
            session.IsRevoked = true;
            await unitOfWork.SaveChanges(cancellationToken);
        }
        return true;
    }

    public async Task<AccountDto> CreateEmployee(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        if (await users.FindByEmailAsync(email) is not null)
            throw new AccountException(HttpStatusCode.Conflict, "An account with this email already exists.");

        return await unitOfWork.ExecuteInTransaction(async _ =>
        {
            var account = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            };
            Check(await users.CreateAsync(account, request.Password));
            await EnsureRolesExist();
            Check(await users.AddToRoleAsync(account, PolicyNames.Employee));
            return new AccountDto(account.Id, email, account.FirstName, account.LastName, IsActive: true, IsAdministrator: false);
        }, cancellationToken);
    }

    public async Task<bool> SetEmployeeActive(SetEmployeeActiveCommand request, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteInTransaction(async token =>
        {
            var account = await db.Users.FromSqlInterpolated($"""SELECT * FROM "HotDeskBooking"."AspNetUsers" WHERE "Id" = {request.Id} FOR UPDATE""")
                .SingleOrDefaultAsync(token) ?? throw new AccountException(HttpStatusCode.NotFound, "User not found.");
            if (await users.IsInRoleAsync(account, PolicyNames.Admin))
                throw new AccountException(HttpStatusCode.Forbidden, "Administrator accounts cannot be modified here.");
            account.IsActive = request.IsActive;
            account.UpdatedAt = clock.UtcNow;
            Check(await users.UpdateAsync(account));
            if (!account.IsActive)
                await db.AuthSessions.Where(x => x.UserId == account.Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRevoked, true), token);
            return true;
        }, cancellationToken);
    }

    public async Task<PagedDto<AccountDto>> ListAccounts(ListAccountsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim()
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
            var pattern = $"%{search}%";
            query = query.Where(x => EF.Functions.ILike(x.Email!, pattern, "\\")
                || EF.Functions.ILike(x.FirstName, pattern, "\\")
                || EF.Functions.ILike(x.LastName, pattern, "\\"));
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AccountDto(x.Id, x.Email ?? string.Empty, x.FirstName, x.LastName, x.IsActive,
                db.UserRoles.Any(link => link.UserId == x.Id
                    && db.Roles.Any(role => role.Id == link.RoleId && role.Name == PolicyNames.Admin))))
            .ToListAsync(cancellationToken);
        return new PagedDto<AccountDto>(items, request.Page, request.PageSize, count);
    }

    private async Task EnsureRolesExist()
    {
        foreach (var role in new[] { PolicyNames.Employee, PolicyNames.Admin })
        {
            if (!await roles.RoleExistsAsync(role))
                Check(await roles.CreateAsync(new IdentityRole<Guid>(role)));
        }
    }

    private static void Check(IdentityResult result)
    {
        if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            throw new AccountException(HttpStatusCode.Conflict, "An account with this email already exists.");
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(error => new ValidationFailure("Account", error.Description)));
    }

    private Task<AuthSession> LockSession(Guid id, CancellationToken cancellationToken) =>
        db.AuthSessions.FromSqlInterpolated($"""SELECT * FROM "HotDeskBooking"."AuthSessions" WHERE "Id" = {id} FOR UPDATE""").SingleAsync(cancellationToken);

    private static string NewRefreshToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static AccountException Unauthorized() => new(HttpStatusCode.Unauthorized, "Invalid credentials or expired session.");
}
