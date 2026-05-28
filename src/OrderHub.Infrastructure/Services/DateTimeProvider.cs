using OrderHub.Application.Common;

namespace OrderHub.Infrastructure.Services;

public sealed class DateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
}
