namespace Domain.Desks;

public interface IDeskRepository
{
    void Add(Desk desk);
    void Delete(Desk desk);
    Task<Desk?> GetByIdAndLocation(Guid id, Guid locationId, CancellationToken cancellationToken = default);
}