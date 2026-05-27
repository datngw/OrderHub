# OrderHub

Central order management API for an e-commerce platform — product catalog, order lifecycle with concurrency-safe stock control, admin reporting, and full observability. Built with .NET 8, Clean Architecture, and PostgreSQL.

---

## Features

- **Authentication** — JWT access tokens (15 min) + refresh tokens (7 days), role-based access (Admin / Customer)
- **Product Catalog** — CRUD with soft delete, paginated list with filtering, search, and sorting
- **Order Management** — Atomic creation with pessimistic locking (`SELECT ... FOR UPDATE`) to prevent overselling, price snapshots, status transitions, cancellation with stock restore
- **Admin Reports** — Top products by revenue, revenue by day, output-cached with tag-based invalidation
- **Observability** — Structured logging (Serilog), distributed tracing, custom business metrics, all via OpenTelemetry + Jaeger
- **API Documentation** — Interactive Scalar UI with OpenAPI spec
- **Production-Ready** — Rate limiting, response compression (Brotli + Gzip), security headers, health probes, API versioning

---

## Architecture

Clean Architecture with strict layer dependencies — each layer only depends on the one below it.

```
Api  →  Infrastructure  →  Application  →  Domain
```

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, enums, value objects, repository interfaces. Zero external dependencies. |
| **Application** | MediatR commands/queries, FluentValidation, DTOs, Result pattern, handler interfaces. |
| **Infrastructure** | EF Core, repository implementations, JWT auth, output caching, OpenTelemetry setup. |
| **Api** | Minimal API endpoints, middleware, DI registration. Entry point only. |

Key patterns in use:

- **CQRS via MediatR** — Separate commands and queries, pipeline behaviors for cross-cutting concerns
- **Specific Repository + Unit of Work** — Focused interfaces per entity, explicit transaction boundaries
- **Pessimistic Locking** — Row-level locks during order creation to guarantee stock correctness under concurrency

---

## Tech Stack

| Area | Technology |
|---|---|
| Runtime | .NET 8 (LTS) |
| Database | PostgreSQL 16 |
| ORM | EF Core 8 + Npgsql |
| Auth | JWT + PasswordHasher\<T\> |
| Validation | FluentValidation |
| Mapping | Mapster |
| CQRS | MediatR |
| Caching | ASP.NET Core Output Caching |
| Logging | Serilog → OTLP |
| Tracing & Metrics | OpenTelemetry SDK |
| Tracing UI | Jaeger |
| API Docs | Scalar + Swashbuckle |
| Versioning | Asp.Versioning (URL segment) |
| Security | NetEscapades headers, rate limiting |
| Testing | xUnit + FluentAssertions + Moq + Testcontainers |
| Containers | Docker + docker-compose |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (for PostgreSQL, Jaeger, or running the full stack)
- Optional: PostgreSQL 16 if running the database locally without Docker

### Run with Docker Compose

The easiest way to get everything running — API, PostgreSQL, pgAdmin, and Jaeger:

```bash
# Clone and configure
git clone <repo-url>
cd OrderHub
cp .env.example .env
# Edit .env with your own secrets

# Start all services
docker-compose up --build
```

| Service | URL |
|---|---|
| API | `http://localhost:5000` |
| Scalar UI | `http://localhost:5000/scalar/v1` |
| pgAdmin | `http://localhost:5050` |

### Run Locally

```bash
# Restore and build
dotnet build OrderHub.slnx

# Apply migrations (set connection string first)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Database=orderhub;Username=postgres;Password=postgres" \
  --project src/OrderHub.Api

dotnet ef database update \
  --project src/OrderHub.Infrastructure \
  --startup-project src/OrderHub.Api

# Run the API
dotnet run --project src/OrderHub.Api
```

### Run Tests

```bash
# All tests
dotnet test OrderHub.slnx

# Unit tests only
dotnet test tests/OrderHub.UnitTests

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/OrderHub.IntegrationTests

# With coverage report
dotnet test --collect:"XPlat Code Coverage"
```

---

## Configuration

Copy `.env.example` to `.env` and fill in your values:

```env
POSTGRES_DB=orderhub
POSTGRES_USER=orderhub
POSTGRES_PASSWORD=<your-strong-password>

PGADMIN_DEFAULT_EMAIL=admin@orderhub.dev
PGADMIN_DEFAULT_PASSWORD=<your-admin-password>

JWT_KEY=<your-min-32-char-secret>
```

When running locally without Docker, use .NET User Secrets or environment variables for sensitive settings. Never commit secrets to source control.

---

## API Endpoints

### Auth

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/auth/register` | No | Register a new user |
| POST | `/api/v1/auth/login` | No | Login, receive tokens |
| POST | `/api/v1/auth/refresh` | No | Refresh access token |
| POST | `/api/v1/auth/logout` | Yes | Revoke refresh token |

### Products

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/products` | No | List products (paginated, filterable, sortable) |
| GET | `/api/v1/products/{id}` | No | Get product detail |
| POST | `/api/v1/products` | Admin | Create product |
| PUT | `/api/v1/products/{id}` | Admin | Update product |
| DELETE | `/api/v1/products/{id}` | Admin | Soft delete product |

### Orders

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/orders` | Customer+ | Create order (atomic stock deduction) |
| GET | `/api/v1/orders/me` | Customer+ | Current user's orders (paginated) |
| GET | `/api/v1/orders/{id}` | Customer+ | Order detail (owner or admin) |
| PUT | `/api/v1/orders/{id}/status` | Admin | Update order status |
| POST | `/api/v1/orders/{id}/cancel` | Customer+ | Cancel order (Pending only, restores stock) |

### Admin Reports

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/admin/reports/top-products` | Admin | Top 10 products by revenue |
| GET | `/api/v1/admin/reports/revenue-by-day` | Admin | Revenue aggregated by day |

### Health

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Liveness probe |
| GET | `/health/ready` | Readiness probe (checks DB connection) |

---

## Observability

All telemetry is exported via **OTLP** to Jaeger.

| Signal | Source | Examples |
|---|---|---|
| Traces | ASP.NET Core, EF Core, custom ActivitySource | Request → Handler → DB → Response |
| Metrics | Runtime instrumentation, custom Meter | `orders.created`, `stock.oversell_attempts`, request duration |
| Logs | Serilog → OTLP sink | Structured logs with `TraceId` + `SpanId` correlation |

---

## Solution Structure

```
OrderHub/
├── src/
│   ├── OrderHub.Domain/           # Entities, enums, interfaces
│   ├── OrderHub.Application/      # Commands, queries, handlers, validators, DTOs
│   ├── OrderHub.Infrastructure/   # EF Core, repositories, auth, caching, OTel
│   └── OrderHub.Api/              # Endpoints, middleware, Program.cs
├── tests/
│   ├── OrderHub.UnitTests/        # Handler and validator tests
│   └── OrderHub.IntegrationTests/ # WebApplicationFactory + Testcontainers
├── docker-compose.yml
├── Dockerfile
├── .env.example
├── OrderHub.slnx
└── OrderHub.http                  # HTTP request file for all endpoints
```

---

## Design Decisions

| Decision | Rationale |
|---|---|
| PostgreSQL over SQL Server | Open-source, mature row-level locking, no licensing cost |
| Pessimistic locking for stock | Guarantees no oversell under concurrency; correctness over throughput |
| Mapster over AutoMapper | Compile-time code generation, less runtime reflection |
| Output Caching over Redis | Built into ASP.NET Core, tagged policies for granular invalidation; Redis available when scaling |
| PasswordHasher\<T\> over BCrypt | Built-in ASP.NET Core, auto-upgradable hash format, no external dependency |
| Specific Repository + Unit of Work | Focused contracts per entity, explicit transaction boundaries, easier to test |
| Serilog + OpenTelemetry | Serilog for structured logging maturity; OpenTelemetry SDK for vendor-neutral tracing and metrics |
| Jaeger over Seq/App Insights | Open-source, native OTLP, lightweight Docker container |

---

## Seed Data

On first migration, two accounts are seeded for testing:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@orderhub.vn` | `Admin@123` |
| Customer | `user@orderhub.vn` | `User@123` |

Change these before deploying to any shared environment.

---

## Roadmap

See [GOALS.md](./GOALS.md) for the full phased implementation roadmap and acceptance criteria.

Planned improvements:

- Idempotency keys for order creation
- Outbox pattern for OrderCreated events
- GitHub Actions CI/CD pipeline
- Redis for distributed caching (multi-instance)
- Background jobs for auto-confirming orders
- Per-user rate limiting

---

## License

Proprietary — Internal use only.
