using Microsoft.Extensions.Logging;

namespace OrderHub.Application.Common.Persistence;

public static class UnitOfWorkExtensions
{
    public static async Task SafeRollbackAsync(
        this IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rollback failed during error handling");
        }
    }
}
