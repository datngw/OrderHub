using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Common.Exceptions;
using OrderHub.Domain.Common;

namespace OrderHub.Api.Middlewares;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (statusCode, title, detail) = MapException(exception, httpContext);

        logger.LogError(exception, "Exception occurred. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = GetProblemType(statusCode),
            Instance = httpContext.Request.Path,
            Detail = detail
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        if (exception is AppException { Error: Domain.Common.ValidationError validationError })
        {
            var errors = validationError.Errors
                .GroupBy(e => e.Code.Split('.')[0])
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            problemDetails.Extensions["errors"] = errors;
        }
        else if (exception is ValidationException validationException && validationException.Errors != null)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            problemDetails.Extensions["errors"] = errors;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }

    private static (int StatusCode, string Title, string? Detail) MapException(
        Exception exception, HttpContext httpContext) => exception switch
    {
        AppException appEx => (MapStatusCode(appEx.Error.Type), appEx.Error.Code, appEx.Error.Description),
        _ => (StatusCodes.Status500InternalServerError, "Server error", GetSafeDetail(exception, httpContext))
    };

    private static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Problem => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string? GetSafeDetail(Exception exception, HttpContext httpContext)
    {
        var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        return env.IsDevelopment() ? exception.Message : null;
    }

    private static string GetProblemType(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };
}
