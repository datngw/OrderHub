using Serilog.Core;
using Serilog.Events;

namespace OrderHub.Api.Logging;

public sealed class SensitiveLogEventFilter : ILogEventFilter
{
    public bool IsEnabled(LogEvent logEvent)
    {
        foreach (var property in logEvent.Properties.Values)
        {
            if (property is ScalarValue { Value: string str }
                && str.Length > 40
                && str.StartsWith("eyJ"))
            {
                return false;
            }
        }

        return true;
    }
}
