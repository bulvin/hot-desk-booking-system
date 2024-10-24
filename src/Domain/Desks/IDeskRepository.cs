namespace Domain.Desks;

public interface IDeskRepository
{
    void Add(Desk desk);
    void Delete(Desk desk);
    void Update(Desk desk);
    Task<bool> Exists(Guid id, CancellationToken cancellationToken = default);
    Task<Desk?> GetByIdAndLocation(Guid id, Guid locationId, CancellationToken cancellationToken = default);
    Task<Desk?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<(List<Desk> Desks, int Count)> GetDesksByLocation(
        Guid locationId,
        bool? isAvailable,
        bool? isBookable,
        DateOnly startDate,
        DateOnly endDate,
        int page, 
        int pageSize,
        CancellationToken cancellationToken);
}