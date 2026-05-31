---
sidebar_position: 3
title: Caching Strategy
description: Handler-level caching with the version-key pattern and stampede protection
---

# Caching Strategy

OrderHub implements an in-process, handler-level caching strategy designed to optimize read latency, protect database resources, and prevent the **Thundering Herd (Cache Stampede)** problem under concurrent traffic spikes.

---

## 1. Core Architectural Caching Choices

1.  **Handler-Level Caching:** Caching logic is implemented inside MediatR query handlers in the Application layer, rather than at the HTTP endpoint level. This allows cached data to be reused across different endpoints (e.g., REST endpoints, background services, or inner pipelines).
2.  **Version-Key Invalidation:** Since standard in-memory caches do not support tag-based invalidation, OrderHub uses a version-key pattern to invalidate cached list queries.
3.  **Cache Stampede Guard:** When a high-traffic cache entry expires, concurrent requests are serialized using a Singleton guard to prevent database connection pool exhaustion.

---

## 2. Version-Key Invalidation Pattern

### How It Works
Every cache key includes a version token:
```
{prefix}:v{version}:{parameters}
```

*   **Read Path:** Generate the cache key using the current version token from the cache. Check the cache.
*   **Cache Hit:** Return the cached data directly (no database query).
*   **Cache Miss:** Query the database, write the result to the cache with a TTL (Time-To-Live), and return the data.
*   **Mutation Path:** Delete the version token from the cache. The next read generates a new random version string, orphaning old cached entries and causing them to expire by their TTL.

### Key Retrieval in Code (`CacheKeys.cs`)
```csharp
public static class CacheKeys
{
    private const string ProductVersionKey = "products:version";

    public static string GetProductVersion(this IMemoryCache cache) =>
        cache.GetOrCreate(ProductVersionKey, entry =>
        {
            entry.SetPriority(CacheItemPriority.NeverRemove)
                 .SetSize(1);
            return Guid.NewGuid().ToString("N")[..8];
        })!;

    public static void InvalidateProducts(this IMemoryCache cache, Guid? productId = null)
    {
        if (productId.HasValue)
            cache.Remove(Products.ById(productId.Value));

        cache.Remove(ProductVersionKey);
    }
}
```

---

## 3. Cache Stampede (Thundering Herd) Protection

When a high-traffic cache entry expires, multiple concurrent requests can attempt to query the database simultaneously, potentially degrading database performance.

OrderHub resolves this using the **`CacheStampedeGuard`**:

```csharp
public sealed class CacheStampedeGuard
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T?> GetOrCreateAsync<T>(
        IMemoryCache cache,
        string key,
        Func<ICacheEntry, Task<T?>> factory,
        Action<ICacheEntry>? configure = null) where T : class
    {
        // 1. Check cache first without lock (Fast Path)
        if (cache.TryGetValue(key, out T? cached))
            return cached;

        // 2. Lock acquisition per cache key
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            // 3. Double-check cache after acquiring lock
            if (cache.TryGetValue(key, out cached))
                return cached;

            // 4. Fetch from database and populate cache (Slow Path)
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

            // 5. Cleanup semaphore from dictionary to prevent memory leaks
            if (semaphore.CurrentCount == 1)
                _locks.TryRemove(key, out _);
        }
    }
}
```

---

## 4. Cache Policies

OrderHub enforces the following caching rules:

| Cache Key Group | Cache Key Format | Sliding Expiration | Absolute Expiration | Invalidation Trigger |
|---|---|:---:|:---:|---|
| **Product List** | `products:list:v{version}:{page}:{size}:{filters}` | 30 seconds | 5 minutes | Any product mutation (Create, Update, Delete). |
| **Product Detail** | `products:byid:{id}` | 30 seconds | 10 minutes | Update or deletion of that specific product ID. |
| **Top Products Report**| `reports:top-products:v{version}:{from}:{to}:{count}`| — | 3 minutes | Creation/cancellation of an order or product mutation. |
| **Daily Revenue Report**| `reports:revenue-by-day:v{version}:{from}:{to}` | — | 3 minutes | Creation/cancellation of an order or product mutation. |

---

## 5. Invalidation Flow Example

When a Customer cancels an order, the `CancelOrderCommandHandler` invalidates the cached products and reports lists:

```csharp
// Commit database changes
await _unitOfWork.SaveChangesAsync(cancellationToken);

// Invalidate caches
_cache.InvalidateProducts();
_cache.InvalidateReports();
```

*   **Result:** The version key `products:version` is removed. The next client request to `GET /api/v1/products` generates a new version token, causing subsequent requests to query the database and cache the updated results.
