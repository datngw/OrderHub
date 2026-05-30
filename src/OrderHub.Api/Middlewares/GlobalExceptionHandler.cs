using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Common.Errors;
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
        return exception switch
        {
            BadHttpRequestException badRequest => await HandleBadRequestAsync(httpContext, badRequest, ct),
            ValidationException validation => await HandleValidationExceptionAsync(httpContext, validation, ct),
            DomainException domain => await HandleDomainExceptionAsync(httpContext, domain, ct),
            _ => await HandleUnexpectedExceptionAsync(httpContext, exception, ct)
        };
    }

    private async ValueTask<bool> HandleValidationExceptionAsync(
        HttpContext httpContext, ValidationException exception, CancellationToken ct)
    {
        logger.LogWarning(exception, "Validation failed");

        var errors = exception.Errors?
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ?? new Dictionary<string, string[]>();

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "Validation error",
            Detail = "One or more validation errors have occurred",
            Instance = httpContext.Request.Path
        };

        return await WriteAsync(httpContext, problemDetails, ct);
    }

    private async ValueTask<bool> HandleDomainExceptionAsync(
        HttpContext httpContext, DomainException exception, CancellationToken ct)
    {
        logger.LogWarning(exception, "Domain error: {Message}", exception.Message);

        var (statusCode, title, typeUri) = ErrorMapping.Map(exception.Error);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Type = typeUri,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        return await WriteAsync(httpContext, problemDetails, ct);
    }

    private async ValueTask<bool> HandleBadRequestAsync(
        HttpContext httpContext, BadHttpRequestException exception, CancellationToken ct)
    {
        logger.LogWarning(exception, "Bad request: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "Bad request",
            Detail = exception.InnerException?.Message ?? exception.Message,
            Instance = httpContext.Request.Path
        };

        return await WriteAsync(httpContext, problemDetails, ct);
    }

    private async ValueTask<bool> HandleUnexpectedExceptionAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "Server error",
            Detail = GetSafeErrorMessage(exception, httpContext),
            Instance = httpContext.Request.Path
        };

        return await WriteAsync(httpContext, problemDetails, ct);
    }

    /// <summary>
    /// In development, expose the full exception message for debugging.
    /// In production, only expose messages from our own domain exceptions — never leak internal details.
    /// </summary>
    private static string? GetSafeErrorMessage(Exception exception, HttpContext httpContext)
    {
        var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        return env.IsDevelopment()
            ? exception.Message
            : null;
    }

    private async ValueTask<bool> WriteAsync(
        HttpContext httpContext, ProblemDetails problemDetails, CancellationToken ct)
    {
        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }
}
