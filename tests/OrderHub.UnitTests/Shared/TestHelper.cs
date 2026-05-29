using Mapster;
using OrderHub.Application;

namespace OrderHub.UnitTests.Shared;

public static class TestHelper
{
    private static readonly object InitLock = new();
    private static bool _initialized;

    public static void EnsureMapsterInitialized()
    {
        lock (InitLock)
        {
            if (_initialized) return;
            TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
            _initialized = true;
        }
    }
}
