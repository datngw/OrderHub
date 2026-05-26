using Microsoft.AspNetCore.Http.HttpResults;
using OrderHub.Application.Common.Results;

namespace OrderHub.Api.Common;

public static class ResultExtensions
{
    public static Results<Ok<T>, ProblemHttpResult> ToResponse<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(
                statusCode: result.Error.StatusCode,
                title: result.Error.Code,
                detail: result.Error.Message);
    }

    public static Results<Created<T>, ProblemHttpResult> ToCreatedResponse<T>(this Result<T> result, string location)
    {
        return result.IsSuccess
            ? TypedResults.Created(location, result.Value)
            : TypedResults.Problem(
                statusCode: result.Error.StatusCode,
                title: result.Error.Code,
                detail: result.Error.Message);
    }

    public static Results<NoContent, ProblemHttpResult> ToNoContentResponse(this Result result)
    {
        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                statusCode: result.Error.StatusCode,
                title: result.Error.Code,
                detail: result.Error.Message);
    }
}
