---
sidebar_position: 2
title: "2. Architecture Constraints"
description: Technical, organizational, and conventions that constrain the architecture
---

# 2. Architecture Constraints

Architectural decisions in OrderHub are shaped by several constraints. These are classified into technical requirements, organizational context, and strict design conventions.

## 2.1 Technical Constraints

Technical constraints dictate the technologies, runtimes, and protocols that must be used in the implementation of the system.

| Category | Constraint | Details & Target | Rationale & Implications |
|---|---|---|---|
| **Runtime Environment** | **.NET 8.0 (LTS)** | The entire backend API must run on the cross-platform .NET 8.0 runtime. | Standardizes development toolchains, leverages modern C# 12 language features, and provides optimized web server (Kestrel) performance. |
| **Persistence Store** | **PostgreSQL 16** | The relational database must be PostgreSQL version 16 (specifically the Alpine Docker image for lightweight packaging). | PostgreSQL offers robust, open-source performance, support for advanced indexing (like GIN Trigram indexes), and mature row-level pessimistic locking (`SELECT ... FOR UPDATE`). |
| **Database Extension** | **`pg_trgm` extension** | PostgreSQL database must enable the `pg_trgm` extension. | Required to support GIN trigram index operators (`gin_trgm_ops`) on the Product Name column, allowing fast case-insensitive search. |
| **Object-Relational Mapper** | **EF Core 8 + Npgsql** | All database transactions and schema migrations must be handled via Entity Framework Core 8 using the Npgsql PostgreSQL provider. | Promotes developer productivity, type-safe query generation, and maintains a clean isolation of database-specific query syntax via LINQ expressions. |
| **API Design Style** | **REST via Minimal APIs** | The API endpoints must follow REST principles, returning JSON payloads and utilizing ASP.NET Core Minimal APIs syntax. | Reduces overhead compared to traditional controller-based APIs, leading to faster startup times, cleaner dependency injection, and easier unit testing. |
| **Security Auth** | **JWT (stateless)** | Authentication must rely on stateless JSON Web Tokens (HS256 signature), utilizing access tokens (15-min) and database-backed refresh tokens (7-day). | Enables horizontal scalability (stateless API nodes) and supports standard security integration for modern single-page applications (SPAs) and mobile clients. |
| **Deployment Standard** | **Docker Containers** | The application and all backing services (DB, logging, admin panels) must compile, package, and deploy inside Docker containers managed by Docker Compose. | Eliminates "works on my machine" issues by ensuring identical environments in local development, continuous integration, and production staging. |

## 2.2 Organizational Constraints

Organizational constraints represent boundaries imposed by team structure, timelines, budgets, and operational limits.

| Constraint | Details & Impact | Architectural Mitigation |
|---|---|---|
| **Team Size** | 1 to 3 developers managing the entire codebase. No dedicated database administrator (DBA) or infrastructure team. | **Keep it simple:** A modular monolith architecture was chosen over a distributed microservice setup. Backing services (PostgreSQL, Seq) run locally in Docker. |
| **Infrastructure Budget** | Minimum operational cost. Development must run smoothly on standard laptops. | **Resource Efficiency:** The backend runs in a single process, utilizing an in-process caching system (`IMemoryCache`) with custom concurrency controls (`CacheStampedeGuard`) to reduce memory and compute footprint. |
| **Operational Lifecycle** | Fast deployment and migration are required. Staging environments must match production configurations. | **Autonomic Migrations:** EF Core migrations are executed programmatically at startup using `DatabaseMigrationHostedService`, eliminating manual database migration steps. |

## 2.3 Architectural Conventions

Architectural conventions are strict coding standards and rules agreed upon by the engineering team to maintain code quality and structural consistency.

### 2.3.1 Asynchronous Execution
*   **Convention:** All network, file, and database input/output (I/O) must run asynchronously using the `async` and `await` keywords.
*   **Constraint:** The use of blocking calls (such as `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) or `async void` (except in background service event loops) is strictly prohibited. This prevents thread pool starvation.

### 2.3.2 Secret Management
*   **Convention:** No credentials, connection strings, or encryption keys may be hardcoded in source files.
*   **Constraint:** Secrets must be injected at runtime using environment variables (in Docker) or .NET User Secrets (in local development). The `.env.example` file serves as the template.

### 2.3.3 API Layer Isolation
*   **Convention:** The API endpoint definitions must remain extremely thin, handling only HTTP routing, input serialization, rate-limiting, and error mapping.
*   **Constraint:** All business processing, database validation, and transactional decisions must occur inside MediatR query/command handlers within the Application layer.

### 2.3.4 Error Representation
*   **Convention:** Business errors (e.g., product not found, insufficient stock) must not throw exceptions. Instead, they must return a `Result<T>` structure encapsulating an `Error` record (code and description).
*   **Constraint:** Unhandled infrastructure exceptions must be intercepted by a `GlobalExceptionHandler` and mapped into standardized **RFC 9457 ProblemDetails** responses, preventing stack traces from leaking to clients.

### 2.3.5 Data Transfer Objects (DTOs)
*   **Convention:** Database entity classes (e.g., `Product`, `Order`) must never be exposed as parameters or return values of public API endpoints.
*   **Constraint:** Separate request DTOs and response DTOs must be created. Mapping between entities and DTOs is handled at the boundary using Mapster compile-time code generation.

### 2.3.6 Security Headers
*   **Convention:** Every HTTP response issued by the API must include security-hardening headers.
*   **Constraint:** NetEscapades security headers middleware is configured to inject HSTS, X-Content-Type-Options, X-Frame-Options (DENY), Content-Security-Policy, and X-XSS-Protection.

### 2.3.7 Dependency Injection Scopes
*   **Convention:** Explicit scope declarations must be configured in `Program.cs`.
*   **Constraint:** Entity Framework `DbContext`, repository implementations, and unit of work instances must be registered as **Scoped** (created once per HTTP request). Custom services, such as the `CacheStampedeGuard`, must be registered as **Singleton**.

## 2.4 Database Performance Constraints

To prevent queries from locking up the PostgreSQL engine or consuming excessive connection pools, the following rules are enforced:

*   **Connection Limits:** The database pool is restricted to a minimum of 5 and a maximum of 100 active connections in the connection string.
*   **Retry on Transient Failure:** Database connections must enable resilient retry execution (`EnableRetryOnFailure`) to recover from momentary network drops.
*   **No-Tracking Reads:** All read-only query paths (Queries in CQRS) must append `.AsNoTracking()` to skip the EF Core entity tracking overhead.
*   **Split Queries:** Complex includes (such as loading an `Order` with its collection of `OrderItems`) must apply `.AsSplitQuery()` to avoid cartesian product performance penalties.
*   **Paginated Lists:** No endpoint is allowed to return an unbound database collection. All lists must use `PagedResult<T>` enforcing `page` and `pageSize` constraints.
