using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderHub.Application.Common.Exceptions;
using Serilog.Context;

namespace OrderHub.Application.Common.Behaviors;

public sealed class LoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        using (LogContext.PushProperty("RequestName", requestName))
        {
            logger.LogInformation("Processing {RequestName}", requestName);

            try
            {
                var response = await next();
                stopwatch.Stop();
                logger.LogInformation("Completed {RequestName} in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
                return response;
            }
            catch (ValidationException ex)
            {
                stopwatch.Stop();
                logger.LogWarning(ex, "Validation failed for {RequestName} after {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (DomainException ex)
            {
                stopwatch.Stop();
                logger.LogWarning(ex, "Domain error in {RequestName} after {ElapsedMs}ms: {Message}", requestName, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "Failed {RequestName} after {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
