using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common.Persistence;

namespace OrderHub.Infrastructure.Persistence.Repositories;

public class UnitOfWork(OrderHubDbContext context, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already in progress. Commit or rollback the current transaction before starting a new one.");
        }

        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        logger.LogDebug("Transaction started");
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            logger.LogWarning("CommitTransactionAsync called without an active transaction");
            return;
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
            logger.LogDebug("Transaction committed");
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            logger.LogWarning("RollbackTransactionAsync called without an active transaction");
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            logger.LogDebug("Transaction rolled back");
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(
            state: (action, cancellationToken),
            operation: async (_, state, ct) =>
            {
                var (act, _) = state;
                _transaction = await context.Database.BeginTransactionAsync(ct);
                try
                {
                    var result = await act(ct);
                    await SaveChangesAsync(ct);
                    await _transaction.CommitAsync(ct);
                    logger.LogDebug("Transaction committed (via ExecuteInTransactionAsync)");
                    return result;
                }
                catch
                {
                    await SafeRollbackInternalAsync(ct);
                    throw;
                }
                finally
                {
                    await DisposeTransactionAsync();
                }
            },
            verifySucceeded: null,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_transaction is not null)
        {
            logger.LogWarning("Disposing UnitOfWork with an active transaction — rolling back");
            try
            {
                await _transaction.RollbackAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during automatic transaction rollback on dispose");
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task SafeRollbackInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                logger.LogDebug("Transaction rolled back");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rollback failed during error handling");
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
