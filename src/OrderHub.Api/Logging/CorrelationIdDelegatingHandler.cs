using Microsoft.AspNetCore.Http;

namespace OrderHub.Api.Logging;

public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?
            .Response.Headers[CorrelationIdHeader].FirstOrDefault();

        if (!string.IsNullOrEmpty(correlationId)
            && !request.Headers.Contains(CorrelationIdHeader))
        {
            request.Headers.Add(CorrelationIdHeader, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
