using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Common.Errors;
using OrderHub.Domain.Common;

namespace OrderHub.Api.Middlewares;

/// <summary>
/// Extensions for building RFC 9457-compliant <see cref="ProblemDetails"/> responses from domain errors.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Creates a <see cref="ProblemDetails"/> from a domain <see cref="Error"/>,
    /// with status code, title, RFC 9110 type URI, and the error message as detail.
    /// </summary>
    public static ProblemDetails ToProblemDetails(this Error error)
    {
        var (statusCode, title, typeUri) = ErrorMapping.Map(error);

        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = typeUri,
            Detail = error.Message
        };
    }
}
