using FluentValidation;

namespace OrderHub.Api.Filters;

public sealed class ValidationEndpointFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title: "Bad request",
                detail: "Request body is required.");
        }

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.ValidationProblem(errors,
                title: "Validation error",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                detail: "One or more validation errors have occurred");
        }

        return await next(context);
    }
}
