namespace OrderHub.Api.Common;

public sealed class CustomProblemDetails
{
    public int Status { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public List<ValidationError>? Errors { get; init; }

    public sealed record ValidationError(string PropertyName, string ErrorMessage);
}

public sealed class CustomProblemResult : IResult
{
    private readonly CustomProblemDetails _details;

    public CustomProblemResult(CustomProblemDetails details)
    {
        _details = details;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = _details.Status;
        return httpContext.Response.WriteAsJsonAsync(_details);
    }
}
