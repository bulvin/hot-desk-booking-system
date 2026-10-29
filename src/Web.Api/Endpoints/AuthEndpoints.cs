using Application.Accounts;
using Application.Accounts.CreateEmployee;
using Application.Accounts.ListAccounts;
using Application.Accounts.Login;
using Application.Accounts.Logout;
using Application.Accounts.Refresh;
using Application.Accounts.SetEmployeeActive;
using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/profile", GetProfile).RequireAuthorization().WithTags("Account")
            .WithSummary("Returns the current profile")
            .ProducesProblem(429).ProducesProblem(401).ProducesProblem(500);

        var auth = endpoints.MapGroup("/api/auth").WithTags("Authentication")
            .RequireRateLimiting("authentication").AddEndpointFilter<AuthOriginFilter>().ProducesProblem(400).ProducesProblem(401)
            .ProducesProblem(403).ProducesProblem(429).ProducesProblem(500);

        auth.MapPost("/login", Login).WithSummary("Sign in using email and password");
        auth.MapPost("/refresh", Refresh).WithSummary("Rotate the HttpOnly refresh cookie");
        auth.MapPost("/logout", Logout).WithSummary("Revoke the current session");

        var users = endpoints.MapGroup("/api/users").RequireAuthorization(PolicyNames.Admin).WithTags("Users")
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(429).ProducesProblem(500);

        users.MapGet("", ListUsers).WithSummary("List accounts");
        users.MapPost("", CreateUser).ProducesProblem(409).WithSummary("Create an employee with an assigned password");
        users.MapPut("/{id:guid}/active", SetActive).ProducesProblem(404).WithSummary("Activate or deactivate an employee");
    }

    private static Ok<ProfileDto> GetProfile(IUserContext userContext) => TypedResults.Ok(userContext.GetCurrentUser());

    private static async Task<Ok<AccessTokenResponse>> Login(LoginCommand request, ISender sender, HttpContext context, CancellationToken cancellationToken)
    {
        var session = await sender.Send(request, cancellationToken);
        // Replace any existing browser session instead of leaving an unreachable active session.
        await sender.Send(new LogoutCommand(AuthCookie.Read(context)), cancellationToken);
        AuthCookie.Set(context, session);
        return TypedResults.Ok(new AccessTokenResponse(session.AccessToken, session.ExpiresAt));
    }

    private static async Task<Ok<AccessTokenResponse>> Refresh(ISender sender, HttpContext context, CancellationToken cancellationToken)
    {
        var session = await sender.Send(new RefreshCommand(AuthCookie.Read(context) ?? string.Empty), cancellationToken);
        AuthCookie.Set(context, session);
        return TypedResults.Ok(new AccessTokenResponse(session.AccessToken, session.ExpiresAt));
    }

    private static async Task<NoContent> Logout(ISender sender, HttpContext context, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(AuthCookie.Read(context)), cancellationToken);
        AuthCookie.Clear(context);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedDto<AccountDto>>> ListUsers([AsParameters] ListAccountsQuery request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(request, cancellationToken));

    private static async Task<Created<AccountDto>> CreateUser(CreateEmployeeCommand request, ISender sender, CancellationToken cancellationToken)
    {
        var user = await sender.Send(request, cancellationToken);
        return TypedResults.Created($"/api/users?search={Uri.EscapeDataString(user.Email)}", user);
    }

    private static async Task<NoContent> SetActive(Guid id, ActiveRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new SetEmployeeActiveCommand(id, request.IsActive), cancellationToken);
        return TypedResults.NoContent();
    }

    internal sealed record AccessTokenResponse(string AccessToken, DateTime ExpiresAt);
    internal sealed record ActiveRequest(bool IsActive);
}
