---
sidebar_position: 6
title: "ADR-005: Query Caching with Version-Key Invalidation and Stampede Protection"
description: Decision to use in-memory caching with version-key invalidation and a custom CacheStampedeGuard for thundering herd prevention
---

# ADR-005: Query Caching with Version-Key Invalidation and Stampede Protection

## Status

✅ Accepted

## Context

Several query endpoints are read-heavy (product listings, admin reports) and benefit from caching to reduce database load and maintain low response latencies.
*   **Invalidation Complexity:** Caches must be invalidated immediately when underlying data changes (e.g., when a product is modified, or an order is created) to prevent serving stale data.
*   **Thundering Herd Risk:** Under high concurrent traffic, when a high-traffic cache entry expires, a "thundering herd" (cache stampede) of concurrent requests can hit the database simultaneously to re-populate the cache, potentially exhausting connection pools and degrading performance.
*   **Single-Instance Monolith Scale:** The solution must be lightweight and operate efficiently within a single-instance container process without requiring external distributed cache infrastructure (like Redis) during the MVP phase.

## Decision

Implement in-memory caching via **`IMemoryCache`**, utilizing a **version-key invalidation pattern** for prefix eviction and a custom **`CacheStampedeGuard`** for thundering herd protection.

### 1. Version-Key Invalidation Pattern
Cache keys for paginated lists incorporate a version token stored in a separate cache entry (e.g., `products:version`):
```
products:list:v{version}:{page}:{pageSize}:{filters}
```
On mutation, the version key is removed from the cache. The next read generates a new random version string, orphaning old cached entries and causing them to expire by their TTL (Time-To-Live).

### 2. Cache Stampede Guard
If a cache miss occurs, requests are serialized per cache key using a Singleton `CacheStampedeGuard` managing `SemaphoreSlim` locks:
```csharp
var cached = await stampedeGuard.GetOrCreateAsync(cache, cacheKey, async entry =>
{
    // Fetch from database
    return await productRepository.GetFilteredAsync(...);
}, entry =>
{
    entry.SetSlidingExpiration(TimeSpan.FromSeconds(30))
         .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
});
```

## Rationale

Four caching approaches were evaluated:

| Criteria | IMemoryCache + version keys + Guard | Redis Distributed Cache | Output Caching | .NET 9 `HybridCache` |
|---|---|---|---|---|
| **External Dependency** | **None** | Redis server | None | None |
| **Invalidation** | Prefix-based via version reset | Tag-based or manual | Tag-based | Tag-based |
| **Stampede Prevention** | ✅ Yes (in-process) | ✅ Yes (using locks) | ❌ No | ✅ Yes (built-in) |
| **Multi-instance Support** | ❌ Per-instance only | ✅ Shared across instances | ❌ Per-instance | ✅ With Redis backplane |
| **Control Level** | Handler-level (domain objects) | Handler-level | Endpoint-level | Handler-level |

This approach was chosen because:
1.  **Zero Infrastructure Dependency:** Runs entirely in-process, keeping deployment simple and resource usage low.
2.  **Atomic Prefix Eviction:** The version-key pattern avoids iterating through thousands of individual cache keys, which is an $O(N)$ operation.
3.  **Prevents Database Overload:** The `CacheStampedeGuard` ensures only a single database query is executed per cache key during a cache miss, serializing concurrent requests.
4.  **Enables Domain Reuse:** Handler-level caching caches domain objects, allowing cached data to be reused across different endpoints (unlike Output Caching, which caches raw HTTP responses).

## Consequences

**Positive:**
*   Provides robust caching and database protection with zero external infrastructure dependencies.
*   Enforces atomic, efficient cache invalidation.
*   Smoothly handles traffic spikes during cache expiration.

**Negative:**
*   **Process-Bound Caching:** Cache is per-instance. Scaling the API horizontally requires migrating to a distributed cache (like Redis) or `.NET 9`'s `HybridCache` to maintain cache coherence.
*   **Thread Blocking:** Requests waiting for the lock consume web server thread resources during database execution.
