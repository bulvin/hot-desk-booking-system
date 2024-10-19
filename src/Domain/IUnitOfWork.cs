namespace Domain;

public interface IUnitOfWork
{
    Task SaveChanges(CancellationToken cancellationToken = default);
}