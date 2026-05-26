namespace OrderHub.Application.Common.Results;

public sealed record Error(string Code, string Message, int StatusCode = 400)
{
    public static Error NotFound(string entityName, object key) =>
        new("NotFound", $"{entityName} with key '{key}' was not found.", 404);

    public static Error Conflict(string message) =>
        new("Conflict", message, 409);

    public static Error Forbidden(string? message = null) =>
        new("Forbidden", message ?? "You do not have permission to perform this action.", 403);

    public static Error Validation(string message) =>
        new("Validation", message, 400);

    public static Error Unauthorized(string message = "Invalid credentials.") =>
        new("Unauthorized", message, 401);
}
