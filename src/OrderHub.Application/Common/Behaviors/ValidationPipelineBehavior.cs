using FluentValidation;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Behaviors;

public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures.Select(f => new Error(
            $"{f.PropertyName}.Invalid",
            f.ErrorMessage,
            ErrorType.Validation)).ToArray();

        var validationError = new ValidationError(errors);

        // If TResponse is Result<T>, return Result<T>.Failure with ValidationError
        // If TResponse is Result, return Result.Failure with ValidationError
        // Otherwise, throw ValidationException for non-Result return types
        var responseType = typeof(TResponse);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(Result<>).MakeGenericType(responseType.GetGenericArguments()[0]);
            var failureMethod = resultType.GetMethod("Failure", [typeof(Error)]);
            return (TResponse)failureMethod!.Invoke(null, [validationError])!;
        }

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(validationError);
        }

        // Fallback: throw for non-Result return types
        throw new Exceptions.ValidationException(
            failures.Select(f => new Exceptions.ValidationError(f.PropertyName, f.ErrorMessage)).ToList());
    }
}
