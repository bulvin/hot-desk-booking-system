namespace Domain.Locations;

public interface ILocationRepository
{
    void Add(Location location);
    void Delete(Location location);
    Task<Location?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsDuplicate(Location location, CancellationToken cancellationToken = default);
}