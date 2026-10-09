namespace Web.Api.Requests;

internal sealed record BookDeskRequest(Guid DeskId, DateOnly StartDate, DateOnly EndDate);
