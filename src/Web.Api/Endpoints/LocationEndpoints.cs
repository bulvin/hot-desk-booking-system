using Application.Browsing;
using Application.Dtos;
using Application.Locations.Browse;
using Application.Locations.Create;
using Application.Locations.Delete;
using Application.Locations.Update;
using Infrastructure.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Web.Api.Requests;

namespace Web.Api.Endpoints;

internal static class LocationEndpoints
{
    internal static void MapLocationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/locations", async (ISender sender, CancellationToken cancellationToken,
                int page = 1, int pageSize = 20) =>
            TypedResults.Ok(await sender.Send(new GetLocationsQuery(new PageRequest(page, pageSize)), cancellationToken)))
            .RequireAuthorization().WithTags("Locations").WithSummary("Browse office locations")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        var group = endpoints.MapGroup("/api/locations")
            .WithTags("Locations")
            .RequireAuthorization(PolicyNames.Admin)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("", Create)
            .WithSummary("Create location")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        group.MapDelete("/{id:guid}", Delete)
            .WithSummary("Delete location")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", Update)
            .WithSummary("Update location")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);
    }

    private static async Task<Created<LocationDto>> Create(CreateLocationRequest request,
        ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new CreateLocationCommand(request.Name, request.Address), cancellationToken);
        return TypedResults.Created($"/api/locations/{response.Id}", response);
    }

    private static async Task<Ok<LocationDto>> Update(Guid id, UpdateLocationRequest request,
        ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new UpdateLocationCommand(id, request.Name, request.Address), cancellationToken);
        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> Delete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteLocationCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
