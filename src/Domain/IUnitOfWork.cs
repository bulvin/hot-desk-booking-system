namespace Domain;

public interface IUnitOfWork
{
    Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

    Task SaveChanges(CancellationToken cancellationToken = default);
}