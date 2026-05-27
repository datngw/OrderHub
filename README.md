# OrderHub — Order Management API

Central order management service for the OrderHub e-commerce platform. Built on .NET 8 with Clean Architecture, JWT authentication, product catalog, order management with concurrency control, admin reporting, and full observability via OpenTelemetry + Jaeger.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                    Client / Frontend                 │
└──────────────────────┬──────────────────────────────┘
                       │ HTTPS
┌──────────────────────▼──────────────────────────────┐
│                  API Layer (Presentation)            │
│   Minimal API Endpoints, Middleware, Filters, Scalar │
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
│   EF Core, Repositories, Auth, Cache, OTel Config   │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│               Domain Layer (Core)                    │
│   Entities, Value Objects, Enums, Interfaces        │
│              (No external dependencies)              │
└─────────────────────────────────────────────────────┘
```

### Request Flow (Create Order)

```
Client → Minimal API Endpoint
       → MediatR (CreateOrderCommand)
       → Validator (FluentValidation)
       → Handler:
           1. Load products with row-level lock (SELECT FOR UPDATE)
           2. Check stock availability
           3. Calculate total server-side
           4. Snapshot UnitPrice in OrderItem
           5. Deduct stock atomically
           6. Create order entity
           7. Save via DbContext (single transaction)
           8. Invalidate report cache
           9. Record business metrics + trace spans
       → Return OrderResponse DTO
```

---

## Tech Stack

| Component        | Choice                              | Reason                                            |
| ---------------- | ----------------------------------- | ------------------------------------------------- |
| Runtime          | .NET 8 (LTS)                        | Long-term support, performance improvements       |
| Web Framework    | ASP.NET Core Minimal API            | Lightweight, delegate-based, no controllers       |
| ORM              | EF Core 8 + Npgsql                  | Mature, LINQ, migration support, PostgreSQL native|
| Database         | PostgreSQL                          | Open-source, row-level locking, JSON support      |
| Auth             | JWT (15 min) + Refresh Token (7d)   | Stateless access, revocable refresh               |
| Password Hash    | PasswordHasher<T> (ASP.NET Core)    | Built-in, PBKDF2 HMAC-SHA256, auto-upgradable     |
| Validation       | FluentValidation                    | Complex rules, testable, separate from models     |
| Mapping          | Mapster                             | Fast, compile-time, less boilerplate              |
| CQRS             | MediatR                             | Decouple handlers, pipeline behaviors             |
| Caching          | Output Caching                      | Built-in ASP.NET Core, tagged policies            |
| Logging          | Serilog + OTLP Sink                 | Structured logging with trace correlation         |
| Tracing          | OpenTelemetry SDK                   | Auto-instrumentation (ASP.NET Core, EF Core, HTTP)|
| Metrics          | OpenTelemetry SDK                   | Runtime + custom business meters                  |
| Tracing Backend  | Jaeger                              | Open-source, native OTLP, Docker-friendly         |
| API Docs         | Scalar + Swashbuckle                | Interactive UI, OpenAPI spec                      |
| API Versioning   | Asp.Versioning                      | URL segment strategy (/api/v1/...)                |
| Security Headers | NetEscapades                        | HSTS, CSP, X-Frame-Options, etc.                  |
| Compression      | Brotli + Gzip                       | Response compression                              |
| Rate Limiting    | ASP.NET Core built-in               | Fixed window, 100 req/min                         |
| Testing          | xUnit + FluentAssertions + Moq      | Integration tests via Testcontainers              |
| Containerization | Docker + docker-compose             | Multi-stage build, App + PostgreSQL + Jaeger      |

---

## Observability Stack

All telemetry signals are exported via **OTLP** to Jaeger (included in docker-compose).

| Signal   | Source                           | What You See in Jaeger                        |
| -------- | -------------------------------- | --------------------------------------------- |
| Traces   | ASP.NET Core, EF Core, HttpClient | Request → Handler → DB query → Response flow  |
| Traces   | Custom ActivitySource            | Business spans: CreateOrder, CancelOrder, etc.|
| Metrics  | Runtime instrumentation          | GC, thread pool, HTTP request duration        |
| Metrics  | Custom Meter                     | orders.created, stock.oversell_attempts, etc.  |
| Logs     | Serilog → OTLP                   | Structured logs with TraceId + SpanId          |

---

## Solution Structure

```
OrderHub/
├── src/
│   ├── OrderHub.Domain/           # Entities, Enums, Value Objects, Interfaces
│   ├── OrderHub.Application/      # Commands, Queries, Handlers, Validators, DTOs
│   ├── OrderHub.Infrastructure/   # EF Core, Repositories, Auth, Cache, OTel Config
│   └── OrderHub.Api/              # Minimal API Endpoints, Middleware, Program.cs
├── tests/
│   ├── OrderHub.UnitTests/        # Application layer unit tests
│   └── OrderHub.IntegrationTests/ # WebApplicationFactory + Testcontainers
├── Directory.Build.props
├── OrderHub.slnx
├── docker-compose.yml
├── Dockerfile
└── OrderHub.http                  # HTTP test file for all endpoints
```

---

## Quick Start

### Prerequisites

- .NET 8 SDK
- Docker & Docker Compose
- (Optional) PostgreSQL 16 if running without Docker

### Run with Docker (recommended)

```bash
docker-compose up --build
```

Services:
- **API**: `https://localhost:8080` — Scalar UI at `/scalar/v1`
- **Jaeger UI**: `http://localhost:16686` — Traces & metrics dashboard

### Run Locally (without Docker)

```bash
# Set connection string via user secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=orderhub;Username=postgres;Password=postgres" --project src/OrderHub.Api

dotnet ef database update --project src/OrderHub.Infrastructure --startup-project src/OrderHub.Api

dotnet run --project src/OrderHub.Api
```

### Run Tests

```bash
# All tests
dotnet test OrderHub.slnx

# Unit tests only
dotnet test tests/OrderHub.UnitTests

# Integration tests only (requires Docker for Testcontainers)
dotnet test tests/OrderHub.IntegrationTests

# With coverage
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

| Method | Endpoint                  | Auth | Description          |
| ------ | ------------------------- | ---- | -------------------- |
| POST   | /api/v1/auth/register     | No   | Register new user    |
| POST   | /api/v1/auth/login        | No   | Login, get tokens    |
| POST   | /api/v1/auth/refresh      | No   | Refresh access token |
| POST   | /api/v1/auth/logout       | Yes  | Revoke refresh token |

### Products (Catalog)

| Method | Endpoint                   | Auth  | Description      |
| ------ | -------------------------- | ----- | ---------------- |
| GET    | /api/v1/products           | No    | List (paginated, filterable, sortable) |
| GET    | /api/v1/products/{id}      | No    | Get detail       |
| POST   | /api/v1/products           | Admin | Create product   |
| PUT    | /api/v1/products/{id}      | Admin | Update product   |
| DELETE | /api/v1/products/{id}      | Admin | Soft delete      |

### Orders

| Method | Endpoint                         | Auth           | Description                                 |
| ------ | -------------------------------- | -------------- | ------------------------------------------- |
| POST   | /api/v1/orders                   | Customer/Admin | Create order (atomic stock deduction)       |
| GET    | /api/v1/orders/me                | Customer/Admin | My orders (paginated)                       |
| GET    | /api/v1/orders/{id}              | Customer/Admin | Order detail (owner or admin)               |
| PUT    | /api/v1/orders/{id}/status       | Admin          | Update status (Confirmed/Shipped/Delivered) |
| POST   | /api/v1/orders/{id}/cancel       | Customer/Admin | Cancel order (Pending only)                 |

### Admin Reports

| Method | Endpoint                                | Auth  | Description            |
| ------ | --------------------------------------- | ----- | ---------------------- |
| GET    | /api/v1/admin/reports/top-products      | Admin | Top 10 by revenue      |
| GET    | /api/v1/admin/reports/revenue-by-day    | Admin | Revenue grouped by day |

### Health

| Method | Endpoint        | Description          |
| ------ | --------------- | -------------------- |
| GET    | /health         | Liveness probe       |
| GET    | /health/ready   | Readiness probe      |

---

## Assumptions

1. **Single service** — OrderHub is one service, not microservices. No inter-service communication needed.
2. **Single currency** — All prices in VND. No multi-currency for MVP.
3. **No payment integration** — Orders are created without payment flow. Payment will be a separate integration later.
4. **No shipping integration** — Order status updates are manual via API (Admin calls PUT /api/v1/orders/{id}/status).
5. **Single device session** — Each refresh token login revokes previous tokens for simplicity. Can extend to multiple sessions later.
6. **Output Caching** — Built-in ASP.NET Core output caching with tagged policies. Redis upgrade path is straightforward when scaling to multiple instances.
7. **Soft delete** — Products use soft delete (IsActive flag). Hard delete for orders is never allowed.
8. **Category as string** — Product.Category is a plain string (e.g. "Electronics", "Clothing"). No separate Category table for MVP.
9. **Price snapshot in OrderItem** — UnitPrice stored at order creation time. Past orders keep original prices regardless of product changes.
10. **Specific Repository + Unit of Work** — Each entity has its own repository interface in Domain, implemented in Infrastructure. IUnitOfWork wraps SaveChanges and transactions.

---

## Trade-offs & Decisions

1. **PostgreSQL over SQL Server** — Open-source, no licensing cost. Row-level locking via `SELECT ... FOR UPDATE` is mature. Better JSON support for future extensibility.
2. **Pessimistic locking (SELECT FOR UPDATE) for stock** — Under high concurrency, optimistic locking would cause many retry failures. Pessimistic lock ensures correctness at the cost of slightly lower throughput. For commerce, oversell is worse than a few milliseconds of lock wait.
3. **Mapster over AutoMapper** — Faster compile-time code generation, less reflection overhead at runtime.
4. **Output Caching over IMemoryCache/Redis** — Built-in ASP.NET Core with tagged policies for granular invalidation. Simpler API than IMemoryCache, fits single-instance deployment. Redis available when scaling.
5. **PasswordHasher<T> over BCrypt** — Built-in ASP.NET Core, no external dependency, auto-upgradable hash format. PBKDF2 with HMAC-SHA256 is sufficient for this scale.
6. **MediatR CQRS** — Commands and queries for clean handler organization, pipeline behaviors for cross-cutting concerns. No full event sourcing — not justified at this scale.
7. **Specific Repository + Unit of Work** — Each entity gets its own focused repository interface. More explicit than generic repository, easier to test and evolve. IUnitOfWork wraps transaction boundaries.
8. **Serilog + OpenTelemetry over pure OTel logging** — Serilog provides rich structured logging with mature sink ecosystem. OpenTelemetry SDK adds vendor-neutral tracing and metrics. `Serilog.Sinks.OpenTelemetry` bridges both worlds via OTLP, so all signals go to one backend (Jaeger).
9. **Jaeger over Seq/Application Insights** — Open-source, native OTLP support, purpose-built for distributed tracing. Lightweight Docker container, no licensing cost.

---

## Future Improvements

- Idempotency keys for POST /api/v1/orders
- Outbox pattern for OrderCreated events
- GitHub Actions CI/CD pipeline
- Redis for distributed caching (multi-instance ready)
- Background job: auto-confirm orders after 5 minutes
- Per-user rate limiting
- Database read replicas for report queries
- BenchmarkDotNet for hot paths

---

## License

Proprietary — OrderHub Internal
