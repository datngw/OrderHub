# CLAUDE.md — OrderHub

> This file guides Claude Code when working on this project. Read it at the start of each session.

## Project Overview

OrderHub is a central order management API for an e-commerce platform. It's a single-service .NET 8 Web API with Clean Architecture, JWT authentication, product catalog, order management with concurrency control, admin reporting, and OpenTelemetry observability (Serilog + Tracing + Metrics + Jaeger).

## Architecture

**Clean Architecture** — 4-project strict layered structure with dependency inversion:

```
Api → Infrastructure → Application → Domain
```

- **Domain** — Entities, enums, repository interfaces, value objects. Zero external dependencies.
- **Application** — MediatR commands/queries, FluentValidation validators, DTOs, Result pattern, handler interfaces, ActivitySource/Meter definitions. Depends only on Domain.
- **Infrastructure** — EF Core DbContext, auth services (JWT/PasswordHasher), repository implementations, observability configuration. Implements Application interfaces.
- **Api** — Minimal API endpoints, middleware, Program.cs, DI registration. Entry point only.

### Key Patterns

- **CQRS via MediatR** — Commands for writes, Queries for reads. All business logic in handlers, never in endpoints.
- **Specific Repository + Unit of Work** — Each entity has its own repository interface (IProductRepository, IUserRepository, etc.) defined in Domain and implemented in Infrastructure. IUnitOfWork wraps SaveChanges and transactions.
- **Separate DTOs** — Request DTOs and response DTOs are always separate from domain entities. Never expose entities to clients.
- **Pessimistic locking** — `SELECT ... FOR UPDATE` on product rows during order creation to prevent overselling.
- **Observability** — Serilog for structured logging, OpenTelemetry SDK for distributed tracing + metrics, Jaeger for visualization. All signals exported via OTLP.
- **IMemoryCache with version-key pattern** — Data-level caching in query handlers. Version keys enable prefix-based invalidation: on mutation, version is reset → old cache entries orphaned → expire by TTL. `CacheKeys` static class centralizes all keys and invalidation logic.
- **Result pattern** — Handlers return `Result<T>` instead of throwing. Domain error collections (`ProductErrors`, `AuthErrors`) define typed `Error` → mapped to HTTP status via `ResultExtensions`.

## Tech Stack

| Component | Choice | Notes |
|-----------|--------|-------|
| Runtime | .NET 8 (LTS) | Target framework |
| Database | PostgreSQL | Via EF Core + Npgsql |
| Auth | JWT (15min) + Refresh Token (7 days) | PasswordHasher<T> (ASP.NET Core built-in) |
| Validation | FluentValidation | Registered via DI |
| Mapping | Mapster | Between entities and DTOs |
| CQRS | MediatR | Pipeline behavior for validation |
| Caching | IMemoryCache | Handler-level data caching, version-key invalidation, SizeLimit 10K entries |
| API Docs | Scalar + Swashbuckle | Scalar UI with OpenAPI spec |
| Logging | Serilog | Structured logging, enrichers, OTLP export |
| Tracing | OpenTelemetry SDK | ASP.NET Core + EF Core + HttpClient auto-instrumentation |
| Metrics | OpenTelemetry SDK | Runtime + custom business meters |
| Tracing Backend | Jaeger | OTLP receiver, Docker container |
| Security Headers | NetEscapades | HSTS, CSP, X-Frame-Options, etc. |
| Versioning | Asp.Versioning | URL segment (/api/v1/...) |
| Compression | Brotli + Gzip | Response compression |
| Rate Limiting | ASP.NET Core built-in | Fixed window, 100 req/min |
| Testing | xUnit + FluentAssertions + Moq | Integration tests use Testcontainers |
| Containers | Docker + docker-compose | Multi-stage build |

## Solution Structure

```
OrderHub/
├── src/
│   ├── OrderHub.Domain/           # Entities, Enums, Interfaces
│   ├── OrderHub.Application/      # Commands, Queries, Handlers, Validators, DTOs, Observability
│   ├── OrderHub.Infrastructure/   # EF Core, Auth Services, Cache, OTel Config
│   └── OrderHub.Api/              # Minimal API Endpoints, Middleware, Program.cs
├── tests/
│   ├── OrderHub.UnitTests/        # Application layer (handlers, validators)
│   └── OrderHub.IntegrationTests/ # WebApplicationFactory + Testcontainers
├── Directory.Build.props
├── OrderHub.slnx
├── docker-compose.yml
├── Dockerfile
└── OrderHub.http
```

## Conventions

### File Organization

- Commands/Queries/Handlers: `Features/{FeatureName}/{Action}/{FileName}.cs`
- Request DTOs: in API layer, grouped in `Endpoints/{Feature}/Requests/` (e.g., `Api/Endpoints/Products/Requests/CreateProductRequest.cs`)
- Response DTOs: at feature-group level if shared (e.g., `Features/Products/ProductResponse.cs`)
- Validators: validate Commands/Queries, co-located with their feature (e.g., `Features/Auth/Register/RegisterCommandValidator.cs`)
- Pipeline behaviors: `Application/Behaviors/`
- Cache keys and invalidation: `Application/Common/Caching/CacheKeys.cs`
- Entity configurations: `Persistence/Configurations/{EntityName}Configuration.cs`

### Naming

- Commands: `{Verb}{Entity}Command` (e.g., `CreateOrderCommand`)
- Queries: `{Verb}{Entity}Query` (e.g., `GetProductsQuery`)
- Handlers: `{Command/Query}Handler` (e.g., `CreateOrderCommandHandler`)
- Request DTOs: `{Verb}{Entity}Request` (e.g., `CreateProductRequest`) — in API layer
- Response DTOs: `{Entity}Response` (e.g., `ProductResponse`) — in Application layer
- Validators: `{Command}Validator` (e.g., `CreateProductCommandValidator`) — validate Commands/Queries
- ActivitySource: `OrderHub.{Feature}` (e.g., `OrderHub.Orders`)
- Meter: `OrderHub.{Feature}` (e.g., `OrderHub.Orders`)

### Code Style

- All I/O must be async — no `.Result`, `.Wait()`, or `async void`
- No hardcoded secrets — use environment variables or user secrets
- Endpoints are thin — only handle HTTP concerns, delegate to MediatR
- DI scopes: Repositories/UnitOfWork/Handlers = Scoped, TokenService = Scoped
- Security headers on all responses: HSTS, X-Content-Type-Options, X-Frame-Options, CSP
- Business errors: handlers return `Result<T>` → `ResultExtensions` maps to ProblemDetails (RFC 9457). No exceptions for expected failures.
- Pipeline/unexpected errors: `GlobalExceptionHandler` catches validation and unhandled exceptions → ProblemDetails (RFC 9457). No stack traces.

### Observability

- **Logging** — Serilog with `Serilog.Enrichers.Span` (TraceId/SpanId correlation), `Serilog.Sinks.OpenTelemetry` (OTLP export)
- **Tracing** — Custom `ActivitySource` per feature for business-level spans; auto-instrumentation for ASP.NET Core, EF Core, HttpClient
- **Metrics** — Custom `Meter` per feature for business counters/histograms (orders.created, stock.oversell_attempts, etc.)
- **Export** — All signals via OTLP to Jaeger (docker-compose)
- No password/token/PII in logs

### Testing

- Unit tests target the Application layer (handlers, validators)
- Integration tests use `WebApplicationFactory` + Testcontainers (PostgreSQL)
- Target ≥ 60% coverage in Application layer

## Commands

```bash
# Build
dotnet build OrderHub.slnx

# Run
dotnet run --project src/OrderHub.Api

# Test (all)
dotnet test OrderHub.slnx

# Test (unit only)
dotnet test tests/OrderHub.UnitTests

# Test (integration only — requires Docker)
dotnet test tests/OrderHub.IntegrationTests

# EF Core migrations
dotnet ef migrations add <Name> --project src/OrderHub.Infrastructure --startup-project src/OrderHub.Api
dotnet ef database update --project src/OrderHub.Infrastructure --startup-project src/OrderHub.Api

# Docker (App + PostgreSQL + Jaeger)
docker-compose up --build
```

## Key Design Decisions

1. **PostgreSQL over SQL Server** — Open-source, mature row-level locking, no licensing cost.
2. **Pessimistic locking for stock** — Prevents oversell under concurrency. Correctness > throughput for commerce.
3. **Mapster over AutoMapper** — Faster compile-time code generation, less reflection.
4. **IMemoryCache over Output Caching** — Handler-level caching stores domain objects, reusable across endpoints. Version-key pattern enables prefix-based invalidation without tag support. Fits single-instance deployment; migrate to HybridCache (.NET 9+) when scaling.
5. **Specific Repository + Unit of Work** — Each entity has its own repository interface (IProductRepository, IUserRepository, IRefreshTokenRepository, IOrderRepository) in Domain/Interfaces/. IUnitOfWork wraps SaveChanges and transactions. Infrastructure/Persistence/Repositories/ contains implementations.
6. **PasswordHasher<T> over BCrypt** — Built-in ASP.NET Core, PBKDF2 with HMAC-SHA256, auto-upgradable hash format, no external dependency.
7. **Category as string** — No separate Category table for MVP. Easy to normalize later.
8. **Price snapshot in OrderItem** — UnitPrice captured at order creation time. Decouples historical data from current prices.
9. **Serilog + OpenTelemetry over pure OTel logging** — Serilog provides rich structured logging with mature ecosystem; OpenTelemetry SDK adds vendor-neutral tracing and metrics. `Serilog.Sinks.OpenTelemetry` bridges both worlds via OTLP.
10. **Jaeger over Seq/Application Insights** — Open-source, native OTLP support, purpose-built for distributed tracing. Lightweight Docker container, no licensing cost.
11. **Result pattern over exceptions** — Business errors use `Result<T>`, exceptions only for validation pipeline and unexpected failures. Both paths → ProblemDetails (RFC 9457).

## Domain Entities

- **Product** — SKU (unique), Name, Description, Price (decimal 18,2), Stock, Category (string), IsActive (soft delete)
- **Order** — UserId, Status (Pending/Confirmed/Shipped/Delivered/Cancelled), TotalAmount, CreatedAt, Items
- **OrderItem** — OrderId, ProductId, Quantity, UnitPrice (snapshot at creation)
- **User** — Email (unique), PasswordHash, FullName, Role (Admin/Customer)
- **RefreshToken** — Token, UserId, ExpiresAt, IsRevoked

## API Endpoints

- `POST /api/v1/auth/register|login|refresh|logout`
- `GET|POST|PUT|DELETE /api/v1/products` (CRUD, Admin for writes, soft delete)
- `POST /api/v1/orders` (create with atomic stock deduction)
- `GET /api/v1/orders/me` (current user's orders)
- `GET /api/v1/orders/{id}` (owner or admin)
- `PUT /api/v1/orders/{id}/status` (Admin: status transitions)
- `POST /api/v1/orders/{id}/cancel` (Pending only, restores stock)
- `GET /api/v1/admin/reports/top-products|revenue-by-day` (cached)
- `GET /health` (liveness) + `GET /health/ready` (readiness)

## Implementation Status

See `GOALS.md` for the full phased roadmap. Phase 1 (Foundation) is complete. Phase 2 (Orders + Tests) and Phase 3 (Production Readiness + Observability) are in progress.
