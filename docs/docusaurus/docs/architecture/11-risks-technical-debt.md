---
sidebar_position: 11
title: "11. Risks and Technical Debt"
description: Known risks, technical debt items, and mitigation roadmap
---

# 11. Risks and Technical Debt

## 11.1 Technical Debt

### TD-001: Single-Instance Cache (IMemoryCache)

| Aspect | Details |
|--------|---------|
| **Current** | `IMemoryCache` is in-process — data is not shared across instances |
| **Impact** | Cannot horizontally scale the API without cache inconsistency |
| **Mitigation** | Migrate to HybridCache (.NET 9+) or Redis distributed cache when scaling to multiple instances |
| **Priority** | P2 (only needed when scaling beyond single instance) |

### TD-002: No Distributed Tracing

| Aspect | Details |
|--------|---------|
| **Current** | Serilog installed with `Serilog.Enrichers.Span` — TraceId/SpanId enrichment ready but no OpenTelemetry SDK |
| **Impact** | Cannot trace requests across service boundaries (when microservices are introduced) |
| **Mitigation** | Add OpenTelemetry SDK + Jaeger/OTLP exporter as next observability step |
| **Priority** | P1 |

### TD-003: No Idempotency for Order Creation

| Aspect | Details |
|--------|---------|
| **Current** | Duplicate order creation requests create duplicate orders |
| **Impact** | Network retries or client bugs could result in duplicate orders |
| **Mitigation** | Add idempotency key header — store key in DB, reject duplicates |
| **Priority** | P2 |

### TD-004: No Outbox Pattern for Events

| Aspect | Details |
|--------|---------|
| **Current** | Order creation is atomic within the database but no events are published |
| **Impact** | Downstream systems (notifications, analytics, shipping) have no way to react to order events |
| **Mitigation** | Implement outbox pattern — write events to Outbox table in same transaction, background worker publishes |
| **Priority** | P2 |

### TD-005: No CI/CD Pipeline

| Aspect | Details |
|--------|---------|
| **Current** | Manual build and deployment |
| **Impact** | No automated quality gates; manual deployment is error-prone |
| **Mitigation** | Set up GitHub Actions: build → test → publish Docker image → deploy |
| **Priority** | P1 |

### TD-006: Category as Free-Form String

| Aspect | Details |
|--------|---------|
| **Current** | Product category is a plain string with no referential integrity |
| **Impact** | Typos and inconsistencies in category names; no category management UI |
| **Mitigation** | Normalize to separate Category table when product management complexity grows |
| **Priority** | P3 |

## 11.2 Architecture Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Single point of failure** — API is a single process | Medium | High | Docker restart policy + health checks → add load balancer when scaling |
| **Cache inconsistency** — Version-key TTL leaves orphaned entries | Low | Low | Orphaned entries expire by TTL (3–10 min max); no stale data served |
| **Database bottleneck** — All queries hit one PostgreSQL instance | Low | Medium | Connection pooling + query optimization already in place; read replicas if needed |
| **Secret management** — Environment variables for secrets | Medium | Medium | Use Docker Secrets or cloud secret management in production |

## 11.3 Mitigation Roadmap

| Phase | Item | Priority |
|-------|------|----------|
| **Next** | OpenTelemetry SDK + Jaeger tracing | P1 |
| **Next** | GitHub Actions CI/CD pipeline | P1 |
| **Later** | Idempotency keys for order creation | P2 |
| **Later** | Outbox pattern for OrderCreated events | P2 |
| **Later** | Redis distributed cache for multi-instance | P2 |
| **Later** | Background jobs for auto-confirming orders | P2 |
| **Future** | Normalize Category to separate table | P3 |
