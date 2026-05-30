using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Errors;

/// <summary>
/// Centralized mapping from domain <see cref="Error"/> to HTTP status codes, titles, and RFC 9110 type URIs.
/// Used by both GlobalExceptionHandler and ResultExtensions to avoid duplication.
/// </summary>
public static class ErrorMapping
{
    public static (int StatusCode, string Title, string TypeUri) Map(Error error)
    {
        return error.Type switch
        {
            ErrorType.NotFound => (404, "Not found", "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ErrorType.Conflict => (409, "Conflict", "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            ErrorType.Unauthorized => (401, "Unauthorized", "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
            ErrorType.Forbidden => (403, "Forbidden", "https://tools.ietf.org/html/rfc9110#section-15.5.4"),
            ErrorType.Validation => (400, "Validation error", "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            _ => (400, "Bad request", "https://tools.ietf.org/html/rfc9110#section-15.5.1")
        };
    }
}
