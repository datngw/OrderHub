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
            catch (ValidationException)
            {
                stopwatch.Stop();
                logger.LogWarning("Validation failed for {RequestName} after {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (AppException ex)
            {
                stopwatch.Stop();
                logger.LogWarning("Request {RequestName} failed with business error: {Code} - {Description} after {ElapsedMs}ms",
                    requestName, ex.Error.Code, ex.Error.Description, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "Unhandled exception in {RequestName} after {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
