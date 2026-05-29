using Microsoft.AspNetCore.Http.HttpResults;
using OrderHub.Api.Middlewares;
using OrderHub.Domain.Common;

namespace OrderHub.Api.Extensions;

public static class ResultExtensions
{
    public static Results<Ok<T>, CustomProblemResult> ToResponse<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : new CustomProblemResult(ToProblemDetails(result.Error));
    }

    public static Results<Created<T>, CustomProblemResult> ToCreatedResponse<T>(this Result<T> result, string location)
    {
        return result.IsSuccess
            ? TypedResults.Created(location, result.Value)
            : new CustomProblemResult(ToProblemDetails(result.Error));
    }

    public static Results<NoContent, CustomProblemResult> ToNoContentResponse(this Result result)
    {
        return result.IsSuccess
            ? TypedResults.NoContent()
            : new CustomProblemResult(ToProblemDetails(result.Error));
    }

    private static CustomProblemDetails ToProblemDetails(Error error) => new()
    {
        Status = MapToStatusCode(error),
        Type = MapToType(error),
        Title = MapToTitle(error),
        Detail = error.Message
    };

    private static int MapToStatusCode(Error error) => error.Type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status400BadRequest
    };

    private static string MapToType(Error error) => error.Type switch
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
