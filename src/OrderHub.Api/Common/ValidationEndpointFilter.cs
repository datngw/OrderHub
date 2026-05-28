using FluentValidation;

namespace OrderHub.Api.Common;

public sealed class ValidationEndpointFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return new CustomProblemResult(new CustomProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "BadRequest",
                Title = "Bad request",
                Detail = "Request body is required.",
                TraceId = context.HttpContext.TraceIdentifier
            });
        }

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .Select(e => new CustomProblemDetails.ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();

            return new CustomProblemResult(new CustomProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "ValidationFailure",
                Title = "Validation error",
                Detail = "One or more validation errors has occurred",
                TraceId = context.HttpContext.TraceIdentifier,
                Errors = errors
            });
        }

        return await next(context);
    }
}
