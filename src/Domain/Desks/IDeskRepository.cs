namespace Domain.Desks;

public interface IDeskRepository
{
    void Add(Desk desk);
    void Delete(Desk desk);
    void Update(Desk desk);
    Task<Desk?> GetByIdAndLocation(Guid id, Guid locationId, CancellationToken cancellationToken = default);
    Task<Desk?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<(List<Desk> Desks, int Count)> GetDesksByLocation(
        Guid locationId,
        bool? isAvailable,
        int page, 
        int pageSize,
        CancellationToken cancellationToken);
}