using Application.Dtos;
using Application.Reservations.BookDesk;
using Application.Reservations.ChangeDesk;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Web.Api.Requests;

namespace Web.Api.Endpoints;

internal static class ReservationEndpoints
{
    internal static void MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/reservations")
            .WithTags("Reservations")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("", BookDesk)
            .WithSummary("Book a desk")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/change-desk", ChangeDesk)
            .WithSummary("Change reserved desk")
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Created<ReservationDto>> BookDesk(BookDeskRequest request,
        ISender sender, CancellationToken cancellationToken)
    {
        var command = new BookDeskCommand(request.DeskId, request.StartDate, request.EndDate);
        var response = await sender.Send(command, cancellationToken);
        return TypedResults.Created($"/api/reservations/{response.Id}", response);
    }

    private static async Task<NoContent> ChangeDesk(Guid id, ChangeDeskRequest request,
        ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeDeskCommand(id, request.DeskId), cancellationToken);
        return TypedResults.NoContent();
    }
}
