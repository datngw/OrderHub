using Microsoft.AspNetCore.Diagnostics;
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
            BadHttpRequestException badRequestException => await HandleBadRequestExceptionAsync(httpContext, badRequestException, ct),
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
            .Select(e => new CustomProblemDetails.ValidationError(e.PropertyName, e.ErrorMessage))
            .ToList();

        var problemDetails = new CustomProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "ValidationFailure",
            Title = "Validation error",
            Detail = "One or more validation errors has occurred",
            TraceId = httpContext.TraceIdentifier,
            Errors = errors
        };

        httpContext.Response.StatusCode = problemDetails.Status;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }

    private async ValueTask<bool> HandleDomainExceptionAsync(
        HttpContext httpContext, DomainException exception, CancellationToken ct)
    {
        _logger.LogWarning(exception, "Domain error: {Message}", exception.Message);

        var statusCode = MapToStatusCode(exception.Error);

        var problemDetails = new CustomProblemDetails
        {
            Status = statusCode,
            Type = MapToErrorType(exception.Error),
            Title = MapToTitle(exception.Error),
            Detail = exception.Message,
            TraceId = httpContext.TraceIdentifier
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }

    private async ValueTask<bool> HandleBadRequestExceptionAsync(
        HttpContext httpContext, BadHttpRequestException exception, CancellationToken ct)
    {
        _logger.LogWarning(exception, "Bad request: {Message}", exception.Message);

        var problemDetails = new CustomProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "BadRequest",
            Title = "Bad request",
            Detail = exception.InnerException?.Message ?? exception.Message,
            TraceId = httpContext.TraceIdentifier
        };

        httpContext.Response.StatusCode = problemDetails.Status;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }

    private async ValueTask<bool> HandleUnexpectedExceptionAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = new CustomProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "ServerError",
            Title = "Server error",
            Detail = "An unexpected error has occurred",
            TraceId = httpContext.TraceIdentifier
        };

        httpContext.Response.StatusCode = problemDetails.Status;

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

    private static string MapToErrorType(Error error) => error.Type switch
    {
        ErrorType.NotFound => "NotFound",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.Validation => "ValidationFailure",
        _ => "BadRequest"
    };

    private static string MapToTitle(Error error) => error.Type switch
    {
        ErrorType.NotFound => "Not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.Validation => "Validation error",
        _ => "Bad request"
    };
}
