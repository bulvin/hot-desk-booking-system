using Application.Dtos;
using Application.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Web.Api.Endpoints;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/profile", GetProfile).RequireAuthorization().WithTags("Account")
            .WithSummary("Returns the current profile")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static Ok<ProfileDto> GetProfile(IUserContext userContext) =>
        TypedResults.Ok(userContext.GetCurrentUser());
}
