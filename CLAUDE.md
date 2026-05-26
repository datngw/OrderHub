# CLAUDE.md — OrderHub

> This file guides Claude Code when working on this project. Read it at the start of each session.

## Project Overview

OrderHub is a central order management API for an e-commerce platform. It's a single-service .NET 8 Web API with Clean Architecture, JWT authentication, product catalog, order management with concurrency control, and admin reporting.

## Architecture

**Clean Architecture** — 4-project strict layered structure with dependency inversion:

```
Api → Infrastructure → Application → Domain
```

- **Domain** — Entities, enums, value objects, domain exceptions. Zero external dependencies.
- **Application** — MediatR commands/queries, FluentValidation validators, DTOs, handler interfaces. Depends only on Domain.
- **Infrastructure** — EF Core DbContext, auth services (JWT/BCrypt), caching implementations. Implements Application interfaces.
- **Api** — Controllers, middleware, Program.cs, DI registration. Entry point only.

### Key Patterns

- **CQRS via MediatR** — Commands for writes, Queries for reads. All business logic in handlers, never in controllers.
- **Direct DbContext** — No repository abstraction. DbContext is injected directly into handlers. It already provides Unit-of-Work and Repository patterns.
- **Separate DTOs** — Request DTOs and response DTOs are always separate from domain entities. Never expose entities to clients.
- **Pessimistic locking** — `SELECT ... FOR UPDATE` on product rows during order creation to prevent overselling.

## Tech Stack

| Component | Choice | Notes |
|-----------|--------|-------|
| Runtime | .NET 8 (LTS) | Target framework |
| Database | PostgreSQL | Via EF Core + Npgsql |
| Auth | JWT (15min) + Refresh Token (7 days) | BCrypt password hashing |
| Validation | FluentValidation | Registered via DI |
| Mapping | Mapster | Between entities and DTOs |
| CQRS | MediatR | Pipeline behavior for validation |
| Caching | IMemoryCache | Sliding 5 min, event-driven invalidation |
| Logging | Serilog | Structured console logging |
| Testing | xUnit + FluentAssertions + Moq | Integration tests use Testcontainers |
| Containers | Docker + docker-compose | Multi-stage build |

## Solution Structure

```
OrderHub/
├── src/
│   ├── OrderHub.Domain/           # Entities, Enums, Interfaces
│   ├── OrderHub.Application/      # Commands, Queries, Handlers, Validators, DTOs
│   ├── OrderHub.Infrastructure/   # EF Core, Auth Services, Cache
│   └── OrderHub.Api/              # Controllers, Middleware, Program.cs
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
- Entity configurations: `Persistence/Configurations/{EntityName}Configuration.cs`

### Naming

- Commands: `{Verb}{Entity}Command` (e.g., `CreateOrderCommand`)
- Queries: `{Verb}{Entity}Query` (e.g., `GetProductsQuery`)
- Handlers: `{Command/Query}Handler` (e.g., `CreateOrderCommandHandler`)
- Request DTOs: `{Verb}{Entity}Request` (e.g., `CreateProductRequest`) — in API layer
- Response DTOs: `{Entity}Response` (e.g., `ProductResponse`) — in Application layer
- Validators: `{Command}Validator` (e.g., `CreateProductCommandValidator`) — validate Commands/Queries

### Code Style

- All I/O must be async — no `.Result`, `.Wait()`, or `async void`
- No hardcoded secrets — use environment variables or user secrets
- Controllers are thin — only handle HTTP concerns, delegate to MediatR
- DI scopes: DbContext/Handlers = Scoped, TokenService = Scoped, Cache = Singleton
- Security headers on all responses: HSTS, X-Content-Type-Options, X-Frame-Options, CSP
- Errors returned as Problem Details (RFC 7807) — no stack traces in production

### Testing

- Unit tests target the Application layer (handlers, validators)
- Integration tests use `WebApplicationFactory` + Testcontainers (PostgreSQL)
- No password/token/PII in logs
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

# Docker
docker-compose up --build
```

## Key Design Decisions

1. **PostgreSQL over SQL Server** — Open-source, mature row-level locking, no licensing cost.
2. **Pessimistic locking for stock** — Prevents oversell under concurrency. Correctness > throughput for commerce.
3. **Mapster over AutoMapper** — Faster compile-time code generation, less reflection.
4. **IMemoryCache over Redis** — Single-instance deployment. Event-driven invalidation in same process.
5. **Direct DbContext over Repository** — EF Core already implements Unit-of-Work and Repository. Extra abstraction adds no value at this scale.
6. **BCrypt over Argon2** — Simpler API, no native dependency, sufficient security with adaptive cost.
7. **Category as string** — No separate Category table for MVP. Easy to normalize later.
8. **Price snapshot in OrderItem** — UnitPrice captured at order creation time. Decouples historical data from current prices.

## Domain Entities

- **Product** — SKU (unique), Name, Description, Price (decimal 18,2), Stock, Category (string), IsActive (soft delete)
- **Order** — UserId, Status (Pending/Confirmed/Shipped/Delivered/Cancelled), TotalAmount, CreatedAt, Items
- **OrderItem** — OrderId, ProductId, Quantity, UnitPrice (snapshot at creation)
- **User** — Email (unique), PasswordHash, FullName, Role (Admin/Customer)
- **RefreshToken** — Token, UserId, ExpiresAt, IsRevoked

## API Endpoints

- `POST /api/auth/register|login|refresh|logout`
- `GET|POST|PUT|DELETE /api/products` (CRUD, Admin for writes, soft delete)
- `POST /api/orders` (create with atomic stock deduction)
- `GET /api/orders/me` (current user's orders)
- `GET /api/orders/{id}` (owner or admin)
- `PUT /api/orders/{id}/status` (Admin: status transitions)
- `POST /api/orders/{id}/cancel` (Pending only, restores stock)
- `GET /api/admin/reports/top-products|revenue-by-day` (cached)
- `GET /health` (liveness + readiness)

## Implementation Priority

See `GOALS.md` for the full 3-day implementation checklist. Day 1 = Foundation + Auth + Catalog, Day 2 = Orders + Reports + Security, Day 3 = Integration Tests + Polish.
