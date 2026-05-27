# OrderHub — Project Goals Checklist (3 Days)

> Status legend: [ ] Not started | [~] In progress | [x] Done
> Priority: **P0** = must-have | **P1** = should-have | **P2** = nice-to-have

---

## Day 1 — Foundation + Auth + Catalog

> Goal: Solution structure up, DB migrated, Auth + Product CRUD working.

### Solution & Domain (P0)

- [x] Create solution with 4 projects: Domain, Application, Infrastructure, Api + configure references
- [x] NuGet packages: EF Core Npgsql, MediatR, FluentValidation, Mapster, Serilog, BCrypt, JWT
- [x] Domain entities: Product (Category as string), Order, OrderItem (with UnitPrice price snapshot), User, enums (OrderStatus, UserRole), base entity
- [x] Domain exception types

### EF Core & Seed (P0)

- [x] DbContext + Fluent API mappings (unique SKU, decimal precision, relationships)
- [x] Direct DbContext usage in handlers — no repository abstraction (documented decision in README)
- [x] Indexes: SKU unique, Product Name + Category + Price (composite for filter/sort), Order UserId, Order CreatedAt, OrderItem ProductId
- [x] Performance target: GET /api/products < 200ms with 100k rows (verify index plan)
- [x] Initial migration
- [x] Seed: 1 admin + 1 customer + 100 products (varied categories and prices)

### Auth (P0)

- [x] POST /api/auth/register — BCrypt hash, FluentValidation (email format, password complexity)
- [x] POST /api/auth/login — validate credentials, return JWT (15min) + refresh token (7 days, stored in DB)
- [x] POST /api/auth/refresh — rotate refresh token, revoke old one
- [x] POST /api/auth/logout — revoke refresh token in DB
- [x] JWT token service + Refresh token service
- [x] Role-based auth ([Authorize(Roles = "Admin")], [Authorize])

### Product API (P0)

- [x] GET /api/products — pagination + filter (category, price range) + search (name) + sort
- [x] GET /api/products/{id}
- [x] POST / PUT / DELETE /api/products (Admin only, soft delete via IsActive flag)
- [x] Separate request/response DTOs (never expose entity directly — anti-mass-assignment)
- [x] FluentValidation for create/update DTOs
- [x] Mapster mapping: Product ↔ ProductResponse

### Cross-cutting (P0)

- [x] Serilog structured logging (console sink)
- [x] Global exception handler → Problem Details (RFC 7807), no stack trace in production
- [x] HTTPS enforcement + security headers (HSTS, X-Content-Type-Options: nosniff, X-Frame-Options: DENY, Content-Security-Policy)
- [x] MediatR pipeline behavior: validation filter that short-circuits on invalid input (fail fast, reject early)

### Docker (P0)

- [x] Dockerfile multi-stage build
- [x] docker-compose.yml (app + PostgreSQL)
- [x] .dockerignore

---

## Day 2 — Orders + Reports + Security

> Goal: Order CRUD done (with concurrency control), status transitions, reports + cache, rate limiting, unit tests.

### Order API (P0)

- [ ] POST /api/orders — create order
  - [ ] Validate product IDs (exist + active) via separate request DTO
  - [ ] Pessimistic lock (SELECT FOR UPDATE) on product rows within transaction
  - [ ] Check stock availability for all items
  - [ ] Calculate total server-side from current product prices (never trust client)
  - [ ] Snapshot UnitPrice in OrderItem at creation time
  - [ ] Deduct stock atomically in same transaction
  - [ ] Invalidate report cache
  - [ ] Return OrderId + status in response DTO
- [ ] GET /api/orders/me — current user's orders, paginated
- [ ] GET /api/orders/{id} — owner or admin only (custom authorization policy)
- [ ] PUT /api/orders/{id}/status — Admin transitions: Pending→Confirmed→Shipped→Delivered (validate allowed transitions)
- [ ] POST /api/orders/{id}/cancel — only Pending status, restore stock in transaction
- [ ] Separate request/response DTOs for all endpoints
- [ ] FluentValidation for CreateOrderRequest + CancelOrder
- [ ] Mapster mapping: Order ↔ OrderResponse

### Admin Reports (P1)

- [ ] GET /api/admin/reports/top-products?from=&to= — top 10 by revenue
- [ ] GET /api/admin/reports/revenue-by-day?from=&to= — revenue grouped by day
- [ ] IMemoryCache (sliding 5 min) + invalidation on order create/cancel

### Rate Limiting (P1)

- [ ] Login: 5 req/min per IP
- [ ] Create order: 10 req/min per user

### Health Check (P1)

- [ ] GET /health — liveness (self) + readiness (DB connection)

### Unit Tests (P0)

- [ ] Auth: password hashing, token generation/validation, register (duplicate email, weak password), login (wrong password, nonexistent user)
- [ ] Order: create happy path, out of stock, product not found, total calculation, cancel happy path, cancel non-Pending (should fail), status transition validation
- [ ] Product: CRUD handler tests (create, update, soft delete)
- [ ] Report: top products, revenue by day, cache hit/miss behavior
- [ ] Target: ≥ 60% coverage in Application layer

---

## Day 3 — Integration Tests + Polish + Docs

> Goal: Integration tests pass (including concurrency anti-oversell), code clean, docs complete.

### Integration Tests (P0)

- [ ] WebApplicationFactory + Testcontainers (PostgreSQL) setup
- [ ] Happy path: Register + Login → tokens
- [ ] Happy path: Create order → verify stock deducted + total correct
- [ ] Happy path: Cancel order → verify stock restored
- [ ] **Concurrency test**: 50 concurrent requests, stock=10 → exactly 10 succeed, 40 fail (no oversell)

### Swagger & API Docs (P0)

- [ ] Swagger/OpenAPI enabled with example request/response for each endpoint
- [ ] Swagger JWT authorize button
- [ ] OrderHub.http file for all endpoints (Auth + Products + Orders + Reports + Health)

### Code Quality Review (P0)

- [ ] No method > 40 lines, no God class
- [ ] Business logic not in Controllers (all in MediatR handlers)
- [ ] DI scopes correct (DbContext/Handlers = Scoped, TokenService = Scoped, Cache = Singleton)
- [ ] No hardcoded secrets — env vars / user secrets only
- [ ] All I/O async — no .Result, .Wait(), async void
- [ ] SQL injection safe (EF Core parameterized queries, no raw SQL for user input)
- [ ] XSS safe (pure API, no HTML rendering, JSON responses with proper content-type)
- [ ] Logging: Info on create/cancel, Warning on out-of-stock, Error on exception
- [ ] No password / token / PII in logs

### Final Documentation (P0)

- [ ] README complete: quick start, accounts, architecture diagram, trade-offs, "if 1 more week"
- [ ] docs/architecture.md — detailed layer descriptions + request flow diagrams
- [ ] Git history clean with meaningful commits

### Bonus (P2 — if time allows, pick 1-2)

- [ ] API versioning (/api/v1/...)
- [ ] Idempotency key for POST /api/orders
- [ ] Outbox pattern for OrderCreated event
- [ ] GitHub Actions CI pipeline

---

## Acceptance Criteria (20 items)

| #   | Criteria                                                        | Priority | Status |
| --- | --------------------------------------------------------------- | -------- | ------ |
| 1   | docker-compose up → app + DB running, Swagger accessible        | P0       | [ ]    |
| 2   | Register + Login → JWT + refresh token                          | P0       | [ ]    |
| 3   | CRUD products (Admin only)                                      | P0       | [ ]    |
| 4   | Product list with pagination, filter, search, sort              | P0       | [ ]    |
| 5   | Create order with atomic stock deduction + price snapshot       | P0       | [ ]    |
| 6   | No oversell under concurrency (tested with 50 req / stock=10)   | P0       | [ ]    |
| 7   | Cancel order restores stock (only Pending)                      | P0       | [ ]    |
| 8   | Order status transitions by Admin (Confirmed/Shipped/Delivered) | P0       | [ ]    |
| 9   | Order history for current user (paginated)                      | P0       | [ ]    |
| 10  | Admin reports with caching + invalidation                       | P1       | [ ]    |
| 11  | Rate limiting on login + order creation                         | P1       | [ ]    |
| 12  | Problem Details errors (RFC 7807, no stack trace leak)          | P0       | [ ]    |
| 13  | Separate request/response DTOs (no entity exposure)             | P0       | [ ]    |
| 14  | Unit test coverage ≥ 60% in Application layer                   | P0       | [ ]    |
| 15  | Integration tests: login, create order, cancel order            | P0       | [ ]    |
| 16  | Concurrency test: 50 requests, stock=10, exactly 10 succeed     | P0       | [ ]    |
| 17  | Health check endpoint                                           | P1       | [ ]    |
| 18  | Structured logging (Serilog: Info/Warning/Error)                | P0       | [ ]    |
| 19  | Security headers + HTTPS                                        | P0       | [ ]    |
| 20  | README with instructions, architecture, trade-offs, assumptions | P0       | [ ]    |
