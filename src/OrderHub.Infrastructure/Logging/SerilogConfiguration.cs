using Serilog;

namespace OrderHub.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static LoggerConfiguration ConfigureInfrastructureDefaults(
        this LoggerConfiguration config)
    {
        config.Destructure.With<SensitiveDataDestructuringPolicy>();
        config.Filter.With<SensitiveLogEventFilter>();
        return config;
    }
}
