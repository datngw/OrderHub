using OrderHub.Application.Common;

namespace OrderHub.Infrastructure.Services;

public sealed class DateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
