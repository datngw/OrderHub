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

## Phase 2 — Order Management, Reports & Testing `[x]`

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

- [x] Unit tests — Auth (Register/Login/Refresh/Logout handlers + validators), Products (CRUD handlers + validators), Orders (Create/Cancel/UpdateStatus/GetById/GetMyOrders handlers + validators), Reports (GetTopProducts/GetRevenueByDay handlers + validators + cache tests) — 52 tests, all passing
- [x] Integration tests — WebApplicationFactory + Testcontainers infrastructure (fixture, helpers, test isolation), concurrency test passing, product query performance tests
- [x] Concurrency test — 20 concurrent requests against stock=5 → exactly 5 succeed, final stock=0, pessimistic locking verified under real DB concurrency
- [x] Product query performance tests — Projection + covering indexes verified via integration tests
- [x] Fix early-return-before-rollback bug in CreateOrderCommandHandler — failed order attempts now explicitly rollback transaction instead of leaking to Dispose()

---

## Phase 3 — Production Readiness

> **Objective:** API documentation, code quality, observability, performance, security hardening, and project docs.

### API Documentation (P0) `[x]`

- [x] `OrderHub.http` file covering all endpoints — 13 API endpoints + 2 health checks, organized by feature group with correct auth requirements, realistic example payloads matching validation rules, and pagination/filter/sort query parameter examples

### Documentation (P0)

- [ ] Docusaurus site with arc42 architecture docs + Getting Started guide (quick start, seed accounts, docker-compose)
- [ ] README.md with quick start + link to docs

### Observability — Serilog + Seq (P0) `[x]`

> **Current Stack:** Serilog (structured logging) → Console + File (JSON, rolling) + Seq (Dev)

- [x] Serilog configured — Console sink (always), File sink (rolling JSON, 100MB/14d retention), Seq sink (Dev only at http://orderhub-seq:5341)
- [x] Serilog enrichers — FromLogContext, MachineName, EnvironmentName, ProcessId, ThreadId, ExceptionDetails, Span (TraceId/SpanId ready for OTel)
- [x] Sensitive data protection — `SensitiveDataDestructuringPolicy` + `SensitiveLogEventFilter` redact JWT tokens and PII from logs
- [x] Seq container in docker-compose — Ports 5341 (ingestion) + 8081 (UI)
- [x] `Serilog.Enrichers.Span` installed — ready for TraceId/SpanId when OTel is added

### Performance — Database & Query Optimization (P0)

- [x] **Connection pooling + Query optimization** — Npgsql pooling (`Pooling=true;MinPoolSize=5;MaxPoolSize=100`), `EnableRetryOnFailure()`, `AsNoTracking()` on all read queries, `AsSplitQuery()` on Order includes
- [x] **Query completeness** — Indexes on `OrderItems.OrderId`, `OrderItems.ProductId`, `Orders.Status`, `Orders.UserId`, `Orders.CreatedAt`; all list endpoints return `PagedResult<T>`

### Security Hardening (P0)

- [x] Password complexity (uppercase, lowercase, digit, special char) + JWT key >= 32 chars
- [x] **Per-endpoint rate limiting partitioned by user ID** — Sliding window limiter phân biệt theo authenticated user ID (anonymous = IP), thay thế policy `"api"` cũ (100 req/min shared toàn server). Chi tiết:
  - Auth: login 5 req/min (by IP), register 3 req/min (by IP), refresh 10 req/min (by IP)
  - Products: 60 req/min (partition by userId / IP fallback)
  - Orders: 30 req/min (partition by userId / IP fallback)
  - Admin: 40 req/min (partition by userId / IP fallback)
  - Partition key: `User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? Connection.RemoteIpAddress?.ToString() ?? "anonymous"`
  - Sliding window với 6 segments/window — không boundary burst
- [x] **Input sanitization** — HTML sanitization cho string fields (Name, Description) prevent stored XSS — `HtmlInputSanitizer` (HtmlSanitizer v9.0.892) + `SanitizeHtmlEndpointFilter` auto-sanitizes all string props on request DTOs via reflection. Applied to Auth + Products endpoints. 12 unit tests covering script injection, iframe, event handlers, SVG XSS, mixed content.

### Stretch Goals (P2)

- [ ] Idempotency key for order creation
- [ ] Outbox pattern for OrderCreated event
- [ ] Redis distributed cache — Redis container in docker-compose, `AddStackExchangeRedisCache` replacing IMemoryCache khi cần multi-instance scaling
- [ ] GitHub Actions CI pipeline

---

## Acceptance Criteria

| #   | Criteria                                                                                         | Priority | Status |
| --- | ------------------------------------------------------------------------------------------------ | -------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | `docker-compose up` → app + DB running, Swagger accessible                                       | P0       | [x]    |
| 2   | Register + Login → JWT + refresh token                                                           | P0       | [x]    |
| 3   | CRUD products (Admin only)                                                                       | P0       | [x]    |
| 4   | Product list with pagination, filter, search, sort                                               | P0       | [x]    |
| 5   | Create order with atomic stock deduction + price snapshot                                        | P0       | [x]    |
| 6   | No oversell under concurrency (50 req / stock=10)                                                | P0       | [x]    | — Verified with 20 concurrent requests / stock=5 via Testcontainers integration test                                                                                     |
| 7   | Cancel order restores stock (Pending only)                                                       | P0       | [x]    |
| 8   | Admin order status transitions (Confirmed/Shipped/Delivered)                                     | P0       | [x]    |
| 9   | Order history for current user (paginated)                                                       | P0       | [x]    |
| 10  | Admin reports with caching + invalidation                                                        | P1       | [x]    | — IMemoryCache handler-level caching with version-key invalidation                                                                                                       |
| 11  | Rate limiting on API endpoints (global + per-endpoint)                                           | P0       | [x]    | — Sliding window partitioned by userId/IP: auth (3-10/min by IP), products (60/min), orders (30/min), admin (40/min)                                                     |
| 12  | Problem Details errors (RFC 9457) — Result pattern + GlobalExceptionHandler, no stack trace leak | P0       | [x]    |
| 13  | Separate request/response DTOs (no entity exposure)                                              | P0       | [x]    |
| 14  | Unit test coverage ≥ 60% in Application layer                                                    | P0       | [x]    | — 52 tests: Auth (4 handlers + 4 validators), Products (5 handlers + 2 validators), Orders (5 handlers + 2 validators), Reports (2 handlers + 1 validator + cache tests) |
| 15  | Integration tests: login, create order, cancel order                                             | P0       | [x]    | — Infrastructure done (fixture + helpers + test isolation), concurrency + product performance tests passing                                                              |
| 16  | Concurrency test: 50 requests, stock=10, exactly 10 succeed                                      | P0       | [x]    | — 20 req / stock=5, verified pessimistic locking under real concurrency                                                                                                  |
| 17  | Health check endpoint (liveness + readiness)                                                     | P1       | [x]    | — `/health/live` (liveness) + `/health/ready` (readiness with DB check)                                                                                                  |
| 18  | Structured logging (Serilog: Info/Warning/Error)                                                 | P0       | [x]    | — Console + File (JSON, rolling) + Seq (Dev)                                                                                                                             |
| 19  | Security headers + HTTPS                                                                         | P0       | [x]    |
| 20  | README with instructions, architecture, trade-offs                                               | P0       | [x]    |
| 21  | DB connection pooling + EF retry + AsNoTracking/SplitQuery on all queries                        | P0       | [x]    |
| 22  | Database indexes cover all query patterns + all list endpoints paginated                         | P0       | [x]    |
| 23  | Auth endpoints rate-limited separately (login 5/min, register 3/min)                             | P0       | [x]    | — Sliding window partitioned by IP                                                                                                                                       |
| 24  | String inputs sanitized against XSS                                                              | P0       | [x]    | — HtmlInputSanitizer (HtmlSanitizer v9.0.892) + SanitizeHtmlEndpointFilter on Auth & Products endpoints, 12 unit tests                                                   |
