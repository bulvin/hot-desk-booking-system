namespace Application.Desks.GetPagedByLocation;

public record GetDesksByLocationRequest(
    bool? IsAvailable,
    bool? IsBookable,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Page,
    int? PageSize);