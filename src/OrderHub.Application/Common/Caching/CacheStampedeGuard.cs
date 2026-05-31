using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace OrderHub.Application.Common.Caching;

public sealed class CacheStampedeGuard
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T?> GetOrCreateAsync<T>(
        IMemoryCache cache,
        string key,
        Func<ICacheEntry, Task<T?>> factory,
        Action<ICacheEntry>? configure = null) where T : class
    {
        // Fast path: check cache first without lock
        if (cache.TryGetValue(key, out T? cached))
            return cached;

        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (cache.TryGetValue(key, out cached))
                return cached;

            var result = await cache.GetOrCreateAsync(key, async entry =>
            {
                configure?.Invoke(entry);
                return await factory(entry);
            });

            return result;
        }
        finally
        {
            semaphore.Release();

            // Cleanup: remove semaphore if no waiters to prevent unbounded growth
            if (semaphore.CurrentCount == 1)
                _locks.TryRemove(key, out _);
        }
    }
}
