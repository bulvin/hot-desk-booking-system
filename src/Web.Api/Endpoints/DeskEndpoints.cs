using Application.Desks.ChangeAvailability;
using Application.Desks.Create;
using Application.Desks.Delete;
using Application.Desks.GetDetails;
using Application.Desks.GetPagedByLocation;
using Application.Dtos;
using Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Web.Api.Requests;

namespace Web.Api.Endpoints;

internal static class DeskEndpoints
{
    internal static void MapDeskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/locations/{locationId:guid}/desks")
            .WithTags("Desks")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPut("/{id:guid}", ChangeAvailability)
            .RequireAuthorization(PolicyNames.Admin)
            .WithSummary("Update desk availability")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("", GetForLocation)
            .WithSummary("Get paged desks for location");

        group.MapGet("/{id:guid}", GetDetails)
            .WithName("GetDeskDetails")
            .WithSummary("Get desk details")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("", Create)
            .RequireAuthorization(PolicyNames.Admin)
            .WithSummary("Create desk in location")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        group.MapDelete("/{deskId:guid}", Delete)
            .RequireAuthorization(PolicyNames.Admin)
            .WithSummary("Delete desk")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<NoContent> ChangeAvailability(Guid id, Guid locationId, bool isAvailable,
        ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeDeskAvailabilityCommand(id, locationId, isAvailable), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedDto<DeskDto>>> GetForLocation(Guid locationId,
        [AsParameters] GetDesksByLocationRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetDesksByLocationQuery(locationId,
            new DeskAvailabilityFilter(request.IsAvailable, request.IsBookable),
            new DateRange(request.StartDate, request.EndDate),
            new PaginationFilter(request.Page, request.PageSize));
        var response = await sender.Send(query, cancellationToken);
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<DeskDetailsDto>> GetDetails(Guid id, Guid locationId,
        ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetDeskDetailsQuery(id, locationId), cancellationToken);
        return TypedResults.Ok(response);
    }

    private static async Task<CreatedAtRoute<DeskDto>> Create(Guid locationId, CreateDeskRequest request,
        ISender sender, CancellationToken cancellationToken)
    {
        var command = new CreateDeskCommand(request.Name, request.Description) { LocationId = locationId };
        var response = await sender.Send(command, cancellationToken);
        return TypedResults.CreatedAtRoute(response, "GetDeskDetails", new { locationId = response.LocationId, id = response.Id });
    }

    private static async Task<NoContent> Delete(Guid locationId, Guid deskId,
        ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteDeskCommand(locationId, deskId), cancellationToken);
        return TypedResults.NoContent();
    }
}
