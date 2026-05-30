using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public sealed class ValidationException(IEnumerable<ValidationError> errors)
    : AppException(new Error("General.Validation", "One or more validation errors occurred", ErrorType.Validation))
{
    public IEnumerable<ValidationError>? Errors { get; } = errors;
}
