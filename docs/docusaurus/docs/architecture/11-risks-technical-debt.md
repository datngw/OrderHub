---
sidebar_position: 11
title: "11. Risks and Technical Debt"
description: Known risks, technical debt items, and mitigation roadmap
---

# 11. Risks and Technical Debt

This section lists known technical debts, architectural risks, and the planned mitigation roadmap for OrderHub.

## 11.1 Technical Debt Log

The following log tracks known technical debts in the codebase, detailing their impact and mitigation strategies:

### TD-001: In-Memory, Single-Instance Caching (`IMemoryCache`)
*   **Current State:** Caching is implemented in-process using `.NET`'s local `IMemoryCache`.
*   **Impact:** This configuration prevents horizontal scaling. If the API is scaled to multiple container instances behind a load balancer, each instance will maintain its own independent cache, leading to cache inconsistency across nodes (e.g., node A serving stale products while node B serving fresh data).
*   **Mitigation:** Replace `IMemoryCache` with a distributed caching engine (such as Redis) or integrate `.NET 9`'s `HybridCache` abstraction, which provides local caching backed by a shared Redis distributed cache.
*   **Priority:** **P2** (Required when scaling beyond a single container instance).

### TD-002: Lacks Distributed Tracing Instrumentation
*   **Current State:** The Serilog pipeline is enriched with `CorrelationId` and `TraceId` properties via `Serilog.Enrichers.Span`. However, the OpenTelemetry SDK and OTLP exporters are not configured in the application runtime.
*   **Impact:** Developers cannot trace requests across container boundaries or visualize execution flows using tracing dashboards (e.g., Jaeger, Zipkin, Application Insights).
*   **Mitigation:** Add the OpenTelemetry SDK to `OrderHub.Api` and configure an OTLP exporter in `OrderHub.Infrastructure` to forward traces to a Jaeger collector.
*   **Priority:** **P1** (Critical for production operations).

### TD-003: Lacks Idempotency for Order Creation
*   **Current State:** The order creation endpoint (`POST /api/v1/orders`) does not support idempotency keys. Duplicate checkout requests generate duplicate orders.
*   **Impact:** If a client experiences a network timeout during checkout and retries the request, the API can create duplicate orders for the same items, resulting in duplicate charges and stock inaccuracies.
*   **Mitigation:** Implement an idempotency middleware. Clients send a unique `X-Idempotency-Key` header with write requests. The middleware stores the request hash and response payload in the database or cache. If a duplicate key is received within a 24-hour window, it returns the cached response without re-executing the transaction.
*   **Priority:** **P2** (Required before exposing the checkout API to third-party integrators).

### TD-004: Absence of Transactional Outbox Pattern for Domain Events
*   **Current State:** Order transactions are committed directly to the database. However, the system does not publish domain events (e.g., `OrderCreatedEvent`, `OrderCancelledEvent`) to notify external services.
*   **Impact:** Downstream systems (such as billing services, shipping trackers, or email notification services) cannot react asynchronously to order status changes, preventing integration with an event-driven architecture.
*   **Mitigation:** Implement the Transactional Outbox pattern:
    1.  When a transaction occurs, write a serialized event to an `OutboxMessages` database table within the same transaction.
    2.  Use a background worker (e.g., Quartz.net or Hosted Service) to poll the table, publish pending messages to a message broker (such as RabbitMQ or Kafka), and mark them as processed.
*   **Priority:** **P2** (Required when integration with downstream systems is introduced).

### TD-005: Absence of Automated CI/CD Pipelines
*   **Current State:** Building, testing, and deploying Docker containers is executed manually by developers on their local machines.
*   **Impact:** The deployment process is error-prone and lacks automated quality gates. Code can be deployed without running tests, introducing regressions.
*   **Mitigation:** Set up a GitHub Actions workflow that:
    1.  Runs unit and integration tests on pull requests.
    2.  Builds the multi-stage Docker image upon merging to the `main` branch.
    3.  Pushes the compiled image to a secure container registry.
    4.  Triggers deployment to target servers.
*   **Priority:** **P1** (Required before launch).

### TD-006: Product Category Represented as a Free-Form String
*   **Current State:** The `Category` property on the `Product` entity is a plain string column with no referential constraints.
*   **Impact:** Allows typos and inconsistent category naming (e.g., `Electronics` vs. `electronic`). Prevents filtering or querying categories dynamically for navigation menus.
*   **Mitigation:** Normalize categories into a separate `Categories` table, referencing them via a foreign key `CategoryId` in the `Products` table.
*   **Priority:** **P3** (Can be deferred until product management requirements increase).

---

## 11.2 Architectural Risk Log

The following table documents architectural risks and their corresponding mitigation strategies:

| Risk | Likelihood | Impact | Mitigation Strategy |
|---|---|---|---|
| **Single Point of Failure (SPOF)**<br/>The API process and database run on a single host. | **Medium** | **High** | Configure Docker container restart policies. In production, deploy containers across multiple availability zones using an orchestrator like Kubernetes. |
| **Database Connection Starvation**<br/>Pessimistic locks block connection threads under high concurrency. | **Low** | **High** | Limit connection pooling parameters (Min 5, Max 100). Implement request execution timeouts and configure rate limiters to prevent DB overload. |
| **Cache Version Key Eviction**<br/>The version key expires from cache, orphaning old cache entries. | **Low** | **Low** | Configure the version key with `NeverRemove` cache priority. Orphaned cache entries are left to expire naturally by their TTL. |
| **Secret Leakage**<br/>Credentials or database secrets are committed to version control. | **Low** | **High** | Maintain secret keys in a `.env` file excluded from Git via `.gitignore`. Configure build pipelines to scan for committed credentials. |

---

## 11.3 Mitigation Roadmap

The following roadmap outlines the scheduled resolution of technical debts and risks:

```
                  +---------------------------------------+
                  |           Phase 1: Near-Term          |
                  |     - GitHub Actions CI/CD Pipeline   |
                  |     - OpenTelemetry & Jaeger tracing  |
                  +-------------------+-------------------+
                                      |
                                      v
                  +---------------------------------------+
                  |           Phase 2: Mid-Term           |
                  |     - Idempotency middleware protection|
                  |     - Outbox pattern event publisher  |
                  +-------------------+-------------------+
                                      |
                                      v
                  +---------------------------------------+
                  |           Phase 3: Long-Term          |
                  |     - Redis distributed caching       |
                  |     - Category database normalization |
                  +---------------------------------------+
```
