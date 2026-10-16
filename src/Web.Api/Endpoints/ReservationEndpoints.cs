using Application.Dtos;
using Application.Reservations.BookDesk;
using Application.Reservations.Cancel;
using Application.Reservations.ChangeDesk;
using Mediator;
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
            .WithDescription("Booking dates are inclusive calendar dates. Today and the seven-day advance window use the UTC calendar for every location. Preserve date values without timezone conversion.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/cancel", Cancel)
            .WithSummary("Cancel reservation")
            .WithDescription("Owners must cancel before the start day begins at midnight UTC. Administrators may cancel through the final UTC day. Completed reservations cannot be canceled. Cancellation retains history and frees the entire date range. Authorized repeats succeed without changes.")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/change-desk", ChangeDesk)
            .WithSummary("Change reserved desk")
            .WithDescription("Only reserved bookings can change desks, before the reservation start day measured at midnight UTC for every location.")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<NoContent> Cancel(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelReservationCommand(id), cancellationToken);
        return TypedResults.NoContent();
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
