namespace Application.Dtos;

public record PagedDto<T>(List<T> Items, int Page, int PageSize, int TotalItems)
{
    public bool HasNextPage => Page * PageSize < TotalItems;
    public bool HasPreviousPage => Page > 1;
}