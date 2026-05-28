namespace OrderHub.Application.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
