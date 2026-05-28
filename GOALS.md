# OrderHub — Implementation Roadmap

> Status: `[ ]` Pending | `[~]` In Progress | `[x]` Done

---

## Phase 1 — Foundation & Core Features `[x]`

> **Objective:** Clean Architecture scaffold, authentication flow, product catalog, and cross-cutting concerns fully operational.

- [x] Clean Architecture scaffold — 4-layer solution, NuGet packages, domain model, repository contracts
- [x] Persistence — EF Core DbContext, Fluent API configs, indexes, initial migration, seed data (1 admin + 1 customer + 100 products)
- [x] Authentication & Authorization — Register/Login/Refresh/Logout, JWT (15 min) + refresh token (7 days), PasswordHasher<T>, role-based auth
- [x] Product Catalog — CRUD (Admin-only, soft delete), paginated list with filter/search/sort, CQRS + FluentValidation + Mapster
- [x] Cross-Cutting Concerns — Serilog, global exception handler (RFC 9457 ProblemDetails), Result pattern for business errors, security headers, IMemoryCache handler-level caching with version-key invalidation, response compression, CORS, rate limiting, API versioning, Scalar + Swashbuckle docs, request timeouts
- [x] Containerization — Multi-stage Dockerfile, docker-compose (App + PostgreSQL), .dockerignore
- [x] Password complexity (uppercase, lowercase, digit, special char) + JWT key >= 32 chars validation

---

## Phase 2 — Order Management, Reports & Testing `[~]`

> **Objective:** Full order lifecycle with concurrency-safe stock control, admin reports, and test coverage.

### Order Endpoints (P0)

- [x] `POST /api/v1/orders` — Atomic creation with pessimistic locking (`SELECT ... FOR UPDATE`), server-side total calculation, price snapshot in OrderItem, stock deduction in same transaction, cache invalidation
- [x] `GET /api/v1/orders/me` — Current user's order history, paginated
- [x] `GET /api/v1/orders/{id}` — Owner or admin only (custom authorization policy)
- [x] `PUT /api/v1/orders/{id}/status` — Admin transitions: `Pending → Confirmed → Shipped → Delivered` with validation
- [x] `POST /api/v1/orders/{id}/cancel` — Cancel only if Pending, restore stock in transaction
- [x] Full CQRS layer — Commands, queries, validators, DTOs, Mapster mappings (following Product Catalog patterns)

### Admin Reports (P1)

- [x] `GET /api/v1/admin/reports/top-products?from=&to=` — Top 10 products by revenue
- [x] `GET /api/v1/admin/reports/revenue-by-day?from=&to=` — Revenue aggregated by day
- [x] IMemoryCache handler-level caching — GetProducts (sliding 30s, abs 5 min), GetProductById (sliding 30s, abs 10 min), GetTopProducts (abs 3 min), GetRevenueByDay (abs 3 min)

### Tests (P0)

- [ ] Unit tests — Auth, Order (happy path + edge cases), Product CRUD, Report handlers; coverage ≥ 60% in Application layer
- [ ] Integration tests — WebApplicationFactory + Testcontainers: auth flow, order create (verify stock + total), order cancel (verify stock restored)
- [ ] Concurrency test — 50 concurrent requests against stock=10 → exactly 10 succeed

---

## Phase 3 — Production Readiness

> **Objective:** API documentation, code quality, observability, performance, security hardening, and project docs.

### API Documentation (P0)

- [ ] OpenAPI spec with example request/response for every endpoint + `OrderHub.http` file covering all endpoints

### Code Quality Audit (P0)

- [ ] Business logic in handlers only, DI scopes correct, all I/O async, no hardcoded secrets, no PII in logs

### Documentation (P0)

- [ ] Docusaurus site with arc42 architecture docs + Getting Started guide (quick start, seed accounts, docker-compose)
- [ ] README.md with quick start + link to docs

### Observability — Serilog + OpenTelemetry + Jaeger (P0)

- [ ] OpenTelemetry SDK — Tracing (ASP.NET Core, EF Core, HttpClient auto-instrumentation) + Metrics (runtime + custom business meters: `orders.created`, `orders.cancelled`, `stock.oversell_attempts`, `order.creation.duration_ms`)
- [~] Serilog OTLP export — `Serilog.Sinks.OpenTelemetry` + `Serilog.Enrichers.Span` for TraceId/SpanId correlation in all log entries — Package installed, not yet configured in pipeline
- [ ] Jaeger + Docker config — Jaeger container in `docker-compose.yml` (OTLP port 4317, UI port 16686), `OTEL_*` env vars, `appsettings.json` OpenTelemetry section (enable/disable per environment, sampling rate)

> **Tech Stack:** Serilog (structured logging) + OpenTelemetry SDK (traces + metrics) + Jaeger (visualization) via OTLP export

### Performance — Database & Query Optimization (P0)

- [~] **Connection pooling + Query optimization** — Npgsql pooling (`Pooling=true;MinPoolSize=5;MaxPoolSize=100`), `EnableRetryOnFailure()`, `AsNoTracking()` on all read queries, `AsSplitQuery()` on Order includes — EnableRetryOnFailure + AsNoTracking done, AsSplitQuery not yet
- [~] **Query completeness** — Migration thêm indexes thiếu (`Orders.Status`, `OrderItems.OrderId`, `OrderItems.ProductId`); verify all list endpoints return `PagedResult<T>` — Indexes exist on OrderItems, Orders.Status and list endpoints paginated

### Security Hardening (P0)

- [x] Password complexity (uppercase, lowercase, digit, special char) + JWT key >= 32 chars
- [ ] **Per-endpoint rate limiting** — Stricter limits cho auth endpoints: login 5 req/min, register 3 req/min, refresh 10 req/min; separate policy cho admin endpoints
- [ ] **Input sanitization** — HTML sanitization cho string fields (Name, Description) prevent stored XSS

### Stretch Goals (P2)

- [ ] Idempotency key for order creation
- [ ] Outbox pattern for OrderCreated event
- [ ] Redis distributed cache — Redis container in docker-compose, `AddStackExchangeRedisCache` replacing IMemoryCache khi cần multi-instance scaling
- [ ] GitHub Actions CI pipeline

---

## Acceptance Criteria

| #   | Criteria                                                                                         | Priority | Status |
| --- | ------------------------------------------------------------------------------------------------ | -------- | ------ | ------------------------------------------------------------------ |
| 1   | `docker-compose up` → app + DB running, Swagger accessible                                       | P0       | [x]    |
| 2   | Register + Login → JWT + refresh token                                                           | P0       | [x]    |
| 3   | CRUD products (Admin only)                                                                       | P0       | [x]    |
| 4   | Product list with pagination, filter, search, sort                                               | P0       | [x]    |
| 5   | Create order with atomic stock deduction + price snapshot                                        | P0       | [x]    |
| 6   | No oversell under concurrency (50 req / stock=10)                                                | P0       | [ ]    |
| 7   | Cancel order restores stock (Pending only)                                                       | P0       | [x]    |
| 8   | Admin order status transitions (Confirmed/Shipped/Delivered)                                     | P0       | [x]    |
| 9   | Order history for current user (paginated)                                                       | P0       | [x]    |
| 10  | Admin reports with caching + invalidation                                                        | P1       | [x]    | — IMemoryCache handler-level caching with version-key invalidation |
| 11  | Rate limiting on API endpoints (global + per-endpoint)                                           | P0       | [~]    |
| 12  | Problem Details errors (RFC 9457) — Result pattern + GlobalExceptionHandler, no stack trace leak | P0       | [x]    |
| 13  | Separate request/response DTOs (no entity exposure)                                              | P0       | [x]    |
| 14  | Unit test coverage ≥ 60% in Application layer                                                    | P0       | [ ]    |
| 15  | Integration tests: login, create order, cancel order                                             | P0       | [ ]    |
| 16  | Concurrency test: 50 requests, stock=10, exactly 10 succeed                                      | P0       | [ ]    |
| 17  | Health check endpoint (liveness + readiness)                                                     | P1       | [x]    |
| 18  | Structured logging (Serilog: Info/Warning/Error)                                                 | P0       | [x]    |
| 19  | Security headers + HTTPS                                                                         | P0       | [x]    |
| 20  | README with instructions, architecture, trade-offs                                               | P0       | [x]    |
| 21  | OpenTelemetry traces + business metrics exported to Jaeger via OTLP                              | P0       | [~]    |
| 22  | Serilog logs correlated with traces (TraceId + SpanId)                                           | P0       | [~]    |
| 23  | DB connection pooling + EF retry + AsNoTracking/SplitQuery on all queries                        | P0       | [~]    |
| 24  | Database indexes cover all query patterns + all list endpoints paginated                         | P0       | [~]    |
| 25  | Auth endpoints rate-limited separately (login 5/min, register 3/min)                             | P0       | [ ]    |
| 26  | String inputs sanitized against XSS                                                              | P0       | [ ]    |
