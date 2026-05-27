# OrderHub — Implementation Roadmap

> Status: `[ ]` Pending | `[~]` In Progress | `[x]` Done

---

## Phase 1 — Foundation & Core Features `[x]`

> **Objective:** Clean Architecture scaffold, authentication flow, product catalog, and cross-cutting concerns fully operational.

- [x] Clean Architecture scaffold — 4-layer solution, NuGet packages, domain model, repository contracts
- [x] Persistence — EF Core DbContext, Fluent API configs, indexes, initial migration, seed data (1 admin + 1 customer + 100 products)
- [x] Authentication & Authorization — Register/Login/Refresh/Logout, JWT (15 min) + refresh token (7 days), PasswordHasher<T>, role-based auth
- [x] Product Catalog — CRUD (Admin-only, soft delete), paginated list with filter/search/sort, CQRS + FluentValidation + Mapster
- [x] Cross-Cutting Concerns — Serilog, global exception handler (RFC 7807), Result pattern, security headers, output caching, response compression, CORS, rate limiting, API versioning, Scalar + Swashbuckle docs, request timeouts
- [x] Containerization — Multi-stage Dockerfile, docker-compose (App + PostgreSQL), .dockerignore

---

## Phase 2 — Order Management, Reports & Testing

> **Objective:** Full order lifecycle with concurrency-safe stock control, admin reports, and test coverage.

### Order Endpoints (P0)

- [ ] `POST /api/v1/orders` — Atomic creation with pessimistic locking (`SELECT ... FOR UPDATE`), server-side total calculation, price snapshot in OrderItem, stock deduction in same transaction, cache invalidation
- [ ] `GET /api/v1/orders/me` — Current user's order history, paginated
- [ ] `GET /api/v1/orders/{id}` — Owner or admin only (custom authorization policy)
- [ ] `PUT /api/v1/orders/{id}/status` — Admin transitions: `Pending → Confirmed → Shipped → Delivered` with validation
- [ ] `POST /api/v1/orders/{id}/cancel` — Cancel only if Pending, restore stock in transaction
- [ ] Full CQRS layer — Commands, queries, validators, DTOs, Mapster mappings (following Product Catalog patterns)

### Admin Reports (P1)

- [ ] `GET /api/v1/admin/reports/top-products?from=&to=` — Top 10 products by revenue
- [ ] `GET /api/v1/admin/reports/revenue-by-day?from=&to=` — Revenue aggregated by day
- [ ] Output Caching (5 min) with invalidation on order create/cancel

### Tests (P0)

- [ ] Unit tests — Auth, Order (happy path + edge cases), Product CRUD, Report handlers; coverage ≥ 60% in Application layer
- [ ] Integration tests — WebApplicationFactory + Testcontainers: auth flow, order create (verify stock + total), order cancel (verify stock restored)
- [ ] Concurrency test — 50 concurrent requests against stock=10 → exactly 10 succeed

---

## Phase 3 — Production Readiness

> **Objective:** API documentation, code quality audit, and project docs.

### API Documentation (P0)

- [ ] OpenAPI spec with example request/response for every endpoint
- [ ] Scalar UI with JWT authorize button
- [ ] `OrderHub.http` file covering all endpoints

### Code Quality Audit (P0)

- [ ] Business logic in handlers only, no endpoint logic
- [ ] DI scopes correct (Scoped for DbContext/Handlers/TokenService, Singleton for Cache)
- [ ] All I/O async, no hardcoded secrets, no PII in logs
- [ ] Proper logging levels — Info (create/cancel), Warning (out-of-stock), Error (exceptions)

### Documentation (P0)

- [ ] Docusaurus site with arc42 architecture docs
- [ ] Getting Started guide (quick start, seed accounts, docker-compose)
- [ ] README.md with quick start + link to docs

### Observability — Serilog + OpenTelemetry + Jaeger (P0)

- [ ] Add OpenTelemetry SDK — Tracing (ASP.NET Core, EF Core, HttpClient auto-instrumentation) + Metrics (runtime + custom business meters)
- [ ] Configure `Serilog.Sinks.OpenTelemetry` — Export structured logs via OTLP with trace/span correlation
- [ ] Configure `Serilog.Enrichers.Span` — Auto-attach `TraceId` + `SpanId` to all log entries
- [ ] Custom `ActivitySource` — `OrderHub.Orders`, `OrderHub.Products`, `OrderHub.Auth` for business-level spans
- [ ] Custom `Meter` — `orders.created`, `orders.cancelled`, `stock.oversell_attempts`, `order.creation.duration_ms`
- [ ] Add Jaeger to `docker-compose.yml` (OTLP receiver on port 4317, UI on port 16686)
- [ ] Add `OTEL_*` env vars to `docker-compose.yml` + `.env.example`
- [ ] OpenTelemetry config section in `appsettings.json` (enable/disable per environment, sampling rate)

> **Tech Stack:** Serilog (structured logging) + OpenTelemetry SDK (traces + metrics) + Jaeger (visualization) via OTLP export

### Stretch Goals (P2)

- [ ] Idempotency key for order creation
- [ ] Outbox pattern for OrderCreated event
- [ ] GitHub Actions CI pipeline

---

## Acceptance Criteria

| #   | Criteria                                                                     | Priority | Status |
| --- | ---------------------------------------------------------------------------- | -------- | ------ |
| 1   | `docker-compose up` → app + DB running, Swagger accessible                   | P0       | [x]    |
| 2   | Register + Login → JWT + refresh token                                       | P0       | [x]    |
| 3   | CRUD products (Admin only)                                                   | P0       | [x]    |
| 4   | Product list with pagination, filter, search, sort                           | P0       | [x]    |
| 5   | Create order with atomic stock deduction + price snapshot                    | P0       | [ ]    |
| 6   | No oversell under concurrency (50 req / stock=10)                            | P0       | [ ]    |
| 7   | Cancel order restores stock (Pending only)                                   | P0       | [ ]    |
| 8   | Admin order status transitions (Confirmed/Shipped/Delivered)                 | P0       | [ ]    |
| 9   | Order history for current user (paginated)                                   | P0       | [ ]    |
| 10  | Admin reports with caching + invalidation                                    | P1       | [ ]    |
| 11  | Rate limiting on API endpoints                                               | P1       | [x]    |
| 12  | Problem Details errors (RFC 7807, no stack trace leak)                       | P0       | [x]    |
| 13  | Separate request/response DTOs (no entity exposure)                          | P0       | [x]    |
| 14  | Unit test coverage ≥ 60% in Application layer                                | P0       | [ ]    |
| 15  | Integration tests: login, create order, cancel order                         | P0       | [ ]    |
| 16  | Concurrency test: 50 requests, stock=10, exactly 10 succeed                  | P0       | [ ]    |
| 17  | Health check endpoint (liveness + readiness)                                 | P1       | [x]    |
| 18  | Structured logging (Serilog: Info/Warning/Error)                             | P0       | [x]    |
| 19  | Security headers + HTTPS                                                     | P0       | [x]    |
| 20  | README with instructions, architecture, trade-offs                           | P0       | [ ]    |
| 21  | OpenTelemetry traces exported to Jaeger (request → DB → response)            | P0       | [ ]    |
| 22  | Business metrics visible in Jaeger (orders.created, stock.oversell_attempts) | P0       | [ ]    |
| 23  | Log entries correlated with traces (TraceId + SpanId in Serilog output)      | P0       | [ ]    |
