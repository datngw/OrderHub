using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Common.Exceptions;
using OrderHub.Domain.Common;

namespace OrderHub.Api.Middlewares;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        return exception switch
        {
            ValidationException validationException => await HandleValidationExceptionAsync(httpContext, validationException, ct),
            DomainException domainException => await HandleDomainExceptionAsync(httpContext, domainException, ct),
            _ => await HandleUnexpectedExceptionAsync(httpContext, exception, ct)
        };
    }

    private async ValueTask<bool> HandleValidationExceptionAsync(
        HttpContext httpContext, ValidationException exception, CancellationToken ct)
    {
        _logger.LogWarning(exception, "Validation failed");

        var errors = exception.Errors?
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ?? new Dictionary<string, string[]>();

        var problemDetails = new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }

    private async ValueTask<bool> HandleDomainExceptionAsync(
        HttpContext httpContext, DomainException exception, CancellationToken ct)
    {
        _logger.LogWarning(exception, "Domain error: {Message}", exception.Message);

        var statusCode = MapToStatusCode(exception.Error);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = exception.Error.Code,
            Detail = exception.Message,
            Type = $"https://httpstatuses.com/{statusCode}",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }

    private async ValueTask<bool> HandleUnexpectedExceptionAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = $"https://httpstatuses.com/500",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }

    private static int MapToStatusCode(Error error) => error.Type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status400BadRequest
    };
}
