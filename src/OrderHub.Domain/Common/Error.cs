namespace OrderHub.Domain.Common;

public enum ErrorType
{
    None,
    NotFound,
    Conflict,
    Validation,
    Unauthorized,
    Forbidden
}

public record Error(string Code, string Message, ErrorType Type = ErrorType.None)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided");

    public static Error NotFound(string entityName, object key) =>
        new($"{entityName}.NotFound", $"{entityName} with key '{key}' was not found.", ErrorType.NotFound);

    public static Error Conflict(string message) =>
        new("Conflict", message, ErrorType.Conflict);

    public static Error Forbidden(string? message = null) =>
        new("Forbidden", message ?? "You do not have permission to perform this action.", ErrorType.Forbidden);

    public static Error Validation(string message) =>
        new("Validation", message, ErrorType.Validation);

    public static Error Unauthorized(string message = "Invalid credentials.") =>
        new("Unauthorized", message, ErrorType.Unauthorized);
}
