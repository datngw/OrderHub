using Microsoft.AspNetCore.Http.HttpResults;
using OrderHub.Domain.Common;

namespace OrderHub.Api.Common;

public static class ResultExtensions
{
    public static Results<Ok<T>, ProblemHttpResult> ToResponse<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(
                statusCode: MapToStatusCode(result.Error),
                title: result.Error.Code,
                detail: result.Error.Message);
    }

    public static Results<Created<T>, ProblemHttpResult> ToCreatedResponse<T>(this Result<T> result, string location)
    {
        return result.IsSuccess
            ? TypedResults.Created(location, result.Value)
            : TypedResults.Problem(
                statusCode: MapToStatusCode(result.Error),
                title: result.Error.Code,
                detail: result.Error.Message);
    }

    public static Results<NoContent, ProblemHttpResult> ToNoContentResponse(this Result result)
    {
        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                statusCode: MapToStatusCode(result.Error),
                title: result.Error.Code,
                detail: result.Error.Message);
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
