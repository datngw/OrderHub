---
sidebar_position: 5
title: "ADR-004: IMemoryCache with Version-Key Pattern"
description: Decision to use in-memory caching with version-key invalidation for read-heavy queries
---

# ADR-004: IMemoryCache with Version-Key Pattern

## Status

✅ Accepted — revisit for multi-instance deployment

## Context

Several query endpoints are read-heavy (product lists, admin reports) and benefit from caching to reduce database load. The caching solution needs to support efficient invalidation when underlying data changes. The system currently runs as a single instance.

## Decision

Use **`IMemoryCache`** with a **version-key pattern** for prefix-based cache invalidation. Cache keys include a version component stored in a separate cache entry. On mutation, the version is reset → old cache entries become orphans → they expire by TTL.

## Rationale

Four caching approaches were evaluated:

| Criteria | IMemoryCache + version keys | Redis distributed cache | Output Caching | HybridCache (.NET 9+) |
|----------|---------------------------|------------------------|----------------|----------------------|
| **External dependency** | None | Redis server | None | None (in-memory by default) |
| **Invalidation** | Prefix-based via version reset | Tag-based or manual | Tag-based | Tag-based |
| **Multi-instance** | ❌ Single-instance only | ✅ Shared across instances | ❌ Per-instance | ✅ With Redis backplane |
| **Control level** | Handler-level (domain objects) | Handler-level | Endpoint-level | Handler-level |
| **Setup complexity** | Low | Medium (Redis infrastructure) | Low | Low |

The version-key pattern was chosen because:

1. **No external dependency** — suitable for single-instance deployment without Redis infrastructure
2. **Handler-level caching** stores domain objects that are reusable across endpoints (unlike output caching which stores HTTP responses)
3. **Prefix-based invalidation** via version reset is atomic and efficient — no need to enumerate and remove individual entries
4. **`CacheKeys` static class** centralizes all cache keys and invalidation logic, preventing key drift

This approach is explicitly designed for single-instance deployment. When scaling to multiple instances, migration to **HybridCache (.NET 9+)** or Redis is planned.

## Consequences

**Positive:**
- No external cache dependency (no Redis infrastructure needed)
- Version reset on mutation invalidates entire prefix atomically
- Fits single-instance deployment perfectly
- `CacheKeys` static class centralizes all cache keys and invalidation logic
- Handler-level caching stores domain objects reusable across endpoints

**Negative:**
- Does not work across multiple instances (cache is per-process)
- Must migrate to HybridCache or Redis for multi-instance scaling
- Memory pressure under high cache volume (mitigated by SizeLimit of 10K entries)
