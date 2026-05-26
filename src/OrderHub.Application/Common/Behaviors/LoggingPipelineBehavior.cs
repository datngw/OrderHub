using MediatR;
using Microsoft.Extensions.Logging;

namespace OrderHub.Application.Common.Behaviors;

public sealed class LoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Processing {RequestName}", requestName);

        try
        {
            var response = await next();
            logger.LogInformation("Completed {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed {RequestName}", requestName);
            throw;
        }
    }
}
