---
sidebar_position: 2
title: "2. Architecture Constraints"
description: Technical, organizational, and conventions that constrain the architecture
---

# 2. Architecture Constraints

## 2.1 Technical Constraints

| Constraint | Details | Rationale |
|-----------|---------|-----------|
| Runtime | **.NET 8 (LTS)** | Long-term support release, mature ecosystem |
| Database | **PostgreSQL 16** | Open-source, mature row-level locking, no licensing cost |
| ORM | **EF Core 8 + Npgsql** | First-party ORM with PostgreSQL provider |
| Single service | Monolith — no microservices | MVP scope, single-team development, simple deployment |
| Container runtime | **Docker + docker-compose** | Consistent dev/prod environments |
| API style | **REST** with Minimal APIs | .NET 8 recommended approach, lightweight endpoint definitions |
| Auth | **JWT** (access 15 min + refresh 7 days) | Stateless authentication, standard for SPAs/mobile clients |

## 2.2 Organizational Constraints

| Constraint | Impact |
|-----------|--------|
| Small team (1–3 developers) | Monolith over microservices; simplicity over flexibility |
| Internal / MVP use case | Optimize for development speed; scale later when needed |
| .NET ecosystem | Leverage built-in ASP.NET Core primitives (PasswordHasher, IMemoryCache, rate limiting) |

## 2.3 Architectural Conventions

These conventions are enforced throughout the codebase:

- **All I/O must be async** — no `.Result`, `.Wait()`, or `async void`
- **No hardcoded secrets** — use environment variables or .NET User Secrets
- **Endpoints are thin** — only handle HTTP concerns; delegate all business logic to MediatR handlers
- **Business errors return `Result<T>`** — no exceptions for expected failures; map to RFC 9457 ProblemDetails
- **Separate DTOs** — request and response DTOs are never domain entities
- **Security headers on all responses** — HSTS, X-Content-Type-Options, X-Frame-Options, CSP
- **DI scopes** — Repositories, UnitOfWork, and Handlers are Scoped; TokenService is Scoped

## 2.4 Database Constraints

| Constraint | Value |
|-----------|-------|
| Connection pooling | MinPoolSize=5, MaxPoolSize=100 |
| Retry on failure | Enabled via `EnableRetryOnFailure()` |
| Read optimization | `AsNoTracking()` on all read queries |
| Complex reads | `AsSplitQuery()` on Order includes |
| Indexes | Covering all query patterns (Orders, OrderItems, Products) |
| All list endpoints | Must return `PagedResult<T>` |

## 2.5 Development Environment

| Tool | Version / Notes |
|------|-----------------|
| .NET SDK | 8.0 |
| Docker | Required for PostgreSQL, Seq, and integration tests |
| IDE | Visual Studio 2022, Rider, or VS Code with C# Dev Kit |
| Test runner | `dotnet test` with xUnit |
| API testing | Scalar UI (built-in) or OrderHub.http file |
