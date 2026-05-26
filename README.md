# OrderHub — Order Management API

Central order management service for the OrderHub e-commerce platform. Built on .NET 8 with Clean Architecture, production-ready.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                    Client / Frontend                 │
└──────────────────────┬──────────────────────────────┘
                       │ HTTPS
┌──────────────────────▼──────────────────────────────┐
│                  API Layer (Presentation)            │
│   Controllers, Middleware, Filters, Swagger          │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│              Application Layer (Use Cases)           │
│   Commands, Queries, Validators, DTOs, Interfaces   │
│              (MediatR - CQRS Pattern)               │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│             Infrastructure Layer                     │
│   EF Core, Repositories, Cache, External Services   │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│               Domain Layer (Core)                    │
│   Entities, Value Objects, Domain Events, Enums     │
│              (No external dependencies)              │
└─────────────────────────────────────────────────────┘
```

### Request Flow (Create Order)

```
Client → API Controller
       → MediatR (CreateOrderCommand)
       → Validator (FluentValidation)
       → Handler:
           1. Load products with row-level lock (SELECT FOR UPDATE)
           2. Check stock availability
           3. Calculate total server-side
           4. Deduct stock atomically
           5. Create order entity
           6. Save via DbContext (single transaction)
           7. Invalidate report cache
       → Return OrderResponse DTO
```

---

## Tech Stack

| Component        | Choice                         | Reason                                        |
| ---------------- | ------------------------------ | --------------------------------------------- |
| Runtime          | .NET 8 (LTS)                   | Long-term support, performance improvements   |
| Web Framework    | ASP.NET Core Web API           | Native, minimal overhead                      |
| ORM              | EF Core 8                      | Mature, LINQ, migration support               |
| Database         | PostgreSQL                     | Open-source, row-level locking, JSON support  |
| Auth             | JWT + Refresh Token            | Stateless access, revocable refresh           |
| Password Hash    | BCrypt                         | Battle-tested, adaptive cost                  |
| Validation       | FluentValidation               | Complex rules, testable, separate from models |
| Mapping          | Mapster                        | Fast, compile-time, less boilerplate          |
| CQRS             | MediatR                        | Decouple handlers, pipeline behaviors         |
| Caching          | IMemoryCache                   | Simple, no extra infra; upgradeable to Redis  |
| Logging          | Serilog                        | Structured logging, sinks flexibility         |
| Testing          | xUnit + FluentAssertions + Moq | Industry standard, readable asserts           |
| Containerization | Docker + docker-compose        | Reproducible environments                     |
| Rate Limiting    | ASP.NET Core built-in          | No extra dependency                           |

---

## Solution Structure

```
OrderHub/
├── src/
│   ├── OrderHub.Domain/           # Entities, Enums, Value Objects, Interfaces
│   ├── OrderHub.Application/      # Commands, Queries, Handlers, Validators, DTOs
│   ├── OrderHub.Infrastructure/   # EF Core, Repositories, Cache, Auth services
│   └── OrderHub.Api/              # Controllers, Middleware, Program.cs
├── tests/
│   ├── OrderHub.UnitTests/        # Application layer unit tests
│   └── OrderHub.IntegrationTests/ # WebApplicationFactory + Testcontainers
├── docs/
│   └── architecture.md            # Detailed architecture decisions
├── docker-compose.yml
├── Dockerfile
├── OrderHub.http                  # Bruno/Postman alternative
└── README.md
```

---

## Quick Start

### Prerequisites

- .NET 8 SDK
- Docker & Docker Compose
- (Optional) PostgreSQL 16 if running without Docker

### Run with Docker (1 command)

```bash
docker-compose up --build
```

App will be available at `https://localhost:8080` and Swagger at `https://localhost:8080/swagger`.

### Run Locally (without Docker)

```bash
# Set connection string via environment variable or user secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=orderhub;Username=postgres;Password=postgres" --project src/OrderHub.Api

dotnet ef database update --project src/OrderHub.Infrastructure --startup-project src/OrderHub.Api

dotnet run --project src/OrderHub.Api
```

### Run Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/OrderHub.UnitTests

# Integration tests only (requires Docker for Testcontainers)
dotnet test tests/OrderHub.IntegrationTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Sample Accounts

| Role     | Email             | Password  |
| -------- | ----------------- | --------- |
| Admin    | admin@orderhub.vn | Admin@123 |
| Customer | user@orderhub.vn  | User@123  |

> Passwords are seeded on first migration. Change in production.

---

## API Endpoints Summary

### Auth

| Method | Endpoint           | Auth | Description          |
| ------ | ------------------ | ---- | -------------------- |
| POST   | /api/auth/register | No   | Register new user    |
| POST   | /api/auth/login    | No   | Login, get tokens    |
| POST   | /api/auth/refresh  | No   | Refresh access token |
| POST   | /api/auth/logout   | Yes  | Revoke refresh token |

### Products (Catalog)

| Method | Endpoint           | Auth  | Description      |
| ------ | ------------------ | ----- | ---------------- |
| GET    | /api/products      | No    | List (paginated) |
| GET    | /api/products/{id} | No    | Get detail       |
| POST   | /api/products      | Admin | Create product   |
| PUT    | /api/products/{id} | Admin | Update product   |
| DELETE | /api/products/{id} | Admin | Soft delete      |

### Orders

| Method | Endpoint                | Auth           | Description                                 |
| ------ | ----------------------- | -------------- | ------------------------------------------- |
| POST   | /api/orders             | Customer/Admin | Create order                                |
| GET    | /api/orders/me          | Customer/Admin | My orders (paged)                           |
| GET    | /api/orders/{id}        | Customer/Admin | Order detail (owner or admin)               |
| PUT    | /api/orders/{id}/status | Admin          | Update status (Confirmed/Shipped/Delivered) |
| POST   | /api/orders/{id}/cancel | Customer/Admin | Cancel order (Pending only)                 |

### Admin Reports

| Method | Endpoint                          | Auth  | Description            |
| ------ | --------------------------------- | ----- | ---------------------- |
| GET    | /api/admin/reports/top-products   | Admin | Top 10 by revenue      |
| GET    | /api/admin/reports/revenue-by-day | Admin | Revenue grouped by day |

### Health

| Method | Endpoint | Description          |
| ------ | -------- | -------------------- |
| GET    | /health  | Liveness + readiness |

---

## Assumptions

1. **Single service** — OrderHub is one service, not microservices. No inter-service communication needed.
2. **Single currency** — All prices in VND. No multi-currency for MVP.
3. **No payment integration** — Orders are created without payment flow. Payment will be a separate integration later.
4. **No shipping integration** — Order status updates are manual via API (Admin calls PUT /api/orders/{id}/status).
5. **Single device session** — Each refresh token login revokes previous tokens for simplicity. Can extend to multiple sessions later.
6. **In-memory cache** — For MVP, IMemoryCache is sufficient. Redis upgrade path is straightforward when scaling to multiple instances.
7. **Soft delete** — Products use soft delete (IsActive flag). Hard delete for orders is never allowed.
8. **Category as string** — Product.Category is a plain string (e.g. "Electronics", "Clothing"). No separate Category table for MVP. Easy to normalize later.
9. **OrderItem price snapshot** — OrderItem stores UnitPrice at the time of order creation. If product price changes later, past orders keep the original price.
10. **Direct DbContext** — Using EF Core DbContext directly in handlers (no repository abstraction). At this scale, the repository pattern adds indirection without benefit. DbContext already implements Unit-of-Work and Repository patterns. Can introduce a repository layer if we need to swap ORMs or add cross-cutting query concerns.
11. **Separate DTOs** — Request DTOs and response DTOs are always separate from domain entities. No entity is ever exposed directly to the client. This prevents mass assignment and decouples API contract from domain model.

---

## Trade-offs & Decisions

> Will be documented during implementation. Key decisions include:

1. **PostgreSQL over SQL Server** — Open-source, no licensing cost for startup. Row-level locking via `SELECT ... FOR UPDATE` is mature. Better JSON support for future extensibility.
2. **Pessimistic locking (SELECT FOR UPDATE) for stock deduction** — Under high concurrency, optimistic locking would cause many retry failures. Pessimistic lock ensures correctness at the cost of slightly lower throughput. For a commerce system, oversell is worse than a few milliseconds of lock wait.
3. **Mapster over AutoMapper** — Faster compile-time code generation, less reflection overhead at runtime.
4. **IMemoryCache over Redis** — Simpler deployment for single-instance. Cache invalidation is event-driven within the same process. Trade-off: doesn't scale to multiple instances without switching to Redis.
5. **MediatR CQRS (lite)** — Using MediatR to separate commands and queries for clean handler organization, but not full event sourcing. The added complexity of event sourcing is not justified at this scale.
6. **BCrypt over Argon2** — Sufficient security, built-in salt, simpler API. Argon2 is better for GPU resistance but adds a native dependency that complicates Docker builds.
7. **Direct DbContext over Repository pattern** — EF Core's DbContext already provides Unit-of-Work and Repository. An extra abstraction layer adds complexity without measurable benefit at this scale. Consistent usage across all handlers is more important than the pattern choice itself.
8. **Category as string field** — A normalized Category table is over-engineering for MVP. String-based category supports filtering and grouping. Migration to a lookup table is straightforward if needed later.
9. **Price snapshot in OrderItem** — Storing UnitPrice in OrderItem decouples historical order data from current product price. Essential for audit and revenue reports to remain accurate over time.

---

## If I Had 1 More Week...

- Switch to Redis for distributed caching (multi-instance ready)
- Add Outbox pattern for OrderCreated events (decouple downstream processing)
- Implement idempotency keys for POST /api/orders
- Add OpenTelemetry tracing with Jaeger
- API versioning (/api/v1/...)
- Background job: auto-confirm orders after 5 minutes
- Rate limiting per-user (not just per-endpoint)
- Database read replicas for report queries
- CI/CD pipeline with GitHub Actions
- BenchmarkDotNet for hot paths

---

## License

Proprietary — OrderHub Internal
