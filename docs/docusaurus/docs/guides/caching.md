---
sidebar_position: 3
title: Caching Strategy
description: Handler-level caching with the version-key pattern for prefix-based invalidation
---

# Caching Strategy

## Overview

OrderHub uses **handler-level caching** via `IMemoryCache` with a custom **version-key pattern** for prefix-based invalidation. This approach caches domain objects inside MediatR query handlers, making cached data reusable across any endpoint that calls the same handler.

## Version-Key Pattern

### How It Works

Every cache key includes a version number:

```
{prefix}:v{version}:{parameters}
```

| Step | Action |
|------|--------|
| **Read** | Generate cache key with current version → check IMemoryCache |
| **Hit** | Return cached result directly (no database query) |
| **Miss** | Query database → cache result with TTL → return |
| **Mutation** | Reset the version number → old keys become orphaned → expire by TTL |

### CacheKeys Implementation

The `CacheKeys` static class centralizes all cache keys and invalidation:

```csharp
public static class CacheKeys
{
    private static int _productsVersion;
    private static int _reportsVersion;

    // Product list cache key (includes version + page params)
    public static string ProductsList(int page, int pageSize, string? search,
        string? category, decimal? minPrice, decimal? maxPrice,
        string? sortBy, string sortOrder)
        => $"products:v{_productsVersion}:p{page}:ps{pageSize}:s{search}:c{category}:min{minPrice}:max{maxPrice}:sort{sortBy}:{sortOrder}";

    // Product by ID cache key
    public static string ProductById(Guid id)
        => $"products:v{_productsVersion}:id:{id}";

    // Invalidate all product cache entries
    public static void InvalidateProducts()
        => Interlocked.Increment(ref _productsVersion);

    // Invalidate all report cache entries
    public static void InvalidateReports()
        => Interlocked.Increment(ref _reportsVersion);
}
```

### Why Not Tags?

`IMemoryCache` doesn't support tag-based invalidation (unlike Redis). The version-key pattern achieves the same effect: incrementing the version effectively invalidates all keys with that prefix because new reads will generate keys with the new version.

## Cache Policies

| Data | Cache Key Pattern | Sliding TTL | Absolute TTL | Invalidation Trigger |
|------|------------------|-------------|-------------|---------------------|
| **Product list** | `products:v{n}:{params}` | 30 seconds | 5 minutes | Any product mutation |
| **Product by ID** | `products:v{n}:id:{id}` | 30 seconds | 10 minutes | Any product mutation |
| **Top products report** | `reports:v{n}:top:{from}:{to}` | — | 3 minutes | Order or product mutation |
| **Revenue by day** | `reports:v{n}:revenue:{from}:{to}` | — | 3 minutes | Order or product mutation |

## Handler Integration

Caching lives inside query handlers, not in endpoints:

```csharp
public async Task<Result<PagedResult<ProductResponse>>> Handle(
    GetProductsQuery request, CancellationToken cancellationToken)
{
    var cacheKey = CacheKeys.ProductsList(
        request.Page, request.PageSize, request.Search,
        request.Category, request.MinPrice, request.MaxPrice,
        request.SortBy, request.SortOrder);

    if (_cache.TryGetValue(cacheKey, out PagedResult<ProductResponse>? cached))
        return Result.Success(cached!);

    var result = await _productRepository.GetAllAsync(...);
    var response = result.Adapt<PagedResult<ProductResponse>>();

    var options = new MemoryCacheEntryOptions()
        .SetSlidingExpiration(TimeSpan.FromSeconds(30))
        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
        .SetSize(1);

    _cache.Set(cacheKey, response, options);

    return Result.Success(response);
}
```

## Invalidation Flow

When a mutation occurs (create product, create order, etc.), the handler calls:

```csharp
CacheKeys.InvalidateProducts();   // Resets product version
CacheKeys.InvalidateReports();    // Resets report version
```

This is an **atomic operation** using `Interlocked.Increment`, ensuring thread safety without locks.

## Limitations and Future

| Current | Future |
|---------|--------|
| `IMemoryCache` (in-process) | Redis or HybridCache (.NET 9+) for multi-instance |
| Version-key pattern | Tag-based invalidation (native in Redis) |
| Single API instance | Multiple instances sharing a distributed cache |
| Size limit: 10K entries | Configurable based on available memory |

:::info
The current approach is optimal for a single-instance deployment. When scaling to multiple instances, migrate to a distributed cache (Redis) or the upcoming .NET 9 HybridCache.
:::
