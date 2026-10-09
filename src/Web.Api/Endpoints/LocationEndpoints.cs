using Application.Dtos;
using Application.Locations.Create;
using Application.Locations.Delete;
using Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Web.Api.Requests;

namespace Web.Api.Endpoints;

internal static class LocationEndpoints
{
    internal static void MapLocationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/locations")
            .WithTags("Locations")
            .RequireAuthorization(PolicyNames.Admin);

        group.MapPost("", Create)
            .WithSummary("Create location")
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:guid}", Delete)
            .WithSummary("Delete location")
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<Created<LocationDto>> Create(CreateLocationRequest request,
        ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new CreateLocationCommand(request.Name, request.Address), cancellationToken);
        return TypedResults.Created($"/api/locations/{response.Id}", response);
    }

    private static async Task<NoContent> Delete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteLocationCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
