using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Common.Errors;
using OrderHub.Domain.Common;

namespace OrderHub.Api.Extensions;

public static class ResultExtensions
{
    public static Results<Ok<T>, ProblemHttpResult> ToResponse<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : ToProblem(result.Error);
    }

    public static Results<Created<T>, ProblemHttpResult> ToCreatedResponse<T>(this Result<T> result, string location)
    {
        return result.IsSuccess
            ? TypedResults.Created(location, result.Value)
            : ToProblem(result.Error);
    }

    public static Results<NoContent, ProblemHttpResult> ToNoContentResponse(this Result result)
    {
        return result.IsSuccess
            ? TypedResults.NoContent()
            : ToProblem(result.Error);
    }

    private static ProblemHttpResult ToProblem(Error error)
    {
        var (statusCode, title, typeUri) = ErrorMapping.Map(error);

        return TypedResults.Problem(
            statusCode: statusCode,
            title: title,
            type: typeUri,
            detail: error.Message);
    }
}
