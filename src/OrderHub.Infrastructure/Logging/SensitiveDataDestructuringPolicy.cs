using Serilog.Core;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;

namespace OrderHub.Infrastructure.Logging;

public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordhash", "accesstoken", "refreshtoken", "token",
        "secret", "apikey", "authorization"
    };

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory,
        [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        result = null!;

        if (value is not System.Collections.IDictionary dict)
            return false;

        var sanitized = new Dictionary<string, object?>();
        foreach (System.Collections.DictionaryEntry entry in dict)
        {
            var key = entry.Key?.ToString();
            if (key is null) continue;

            sanitized[key] = SensitivePropertyNames.Contains(key)
                ? "***REDACTED***"
                : entry.Value;
        }

        result = propertyValueFactory.CreatePropertyValue(sanitized, destructureObjects: true);
        return true;
    }
}
