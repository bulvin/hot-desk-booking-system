namespace Domain;

public interface IUnitOfWork
{
    /// <summary>Runs an operation, saves its changes, and commits them together. Exceptions roll back the transaction.</summary>
    Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

    Task SaveChanges(CancellationToken cancellationToken = default);
}