using Application.Users.Login;
using Application.Users.Register;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Web.Api.Requests;

namespace Web.Api.Endpoints;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api")
            .WithTags("Auth")
            .AllowAnonymous()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/register", Register)
            .WithSummary("Creates a new employee account")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithSummary("Logs in a user");
    }

    private static async Task<Ok<Guid>> Register(RegisterUserRequest request,
        ISender sender, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Email, request.FirstName, request.LastName, request.Password);
        var response = await sender.Send(command, cancellationToken);
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<string>> Login(LoginUserRequest request,
        ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new LoginUserCommand(request.Email, request.Password), cancellationToken);
        return TypedResults.Ok(response);
    }
}
