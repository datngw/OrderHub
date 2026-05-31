namespace OrderHub.Application.Common.Persistence;

public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the given action inside a database transaction with automatic retry on transient failures.
    /// This is the recommended way to run operations that require pessimistic locking (FOR UPDATE)
    /// or need retry resilience for deadlocks (PostgreSQL error 40P01).
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
