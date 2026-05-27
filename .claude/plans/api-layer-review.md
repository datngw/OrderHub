# Plan: OrderHub.Api Production-Ready Review & Improvement

**Architecture:** Clean Architecture (Minimal API)
**Affected layers:** Primarily Api, some Application/Infrastructure
**Estimated steps:** 12

---

## Current State Assessment

### What's Already Good
- Clean Architecture with strict dependency inversion (Api → Infrastructure → Application → Domain)
- Minimal API with typed results (`Results<Ok<T>, ProblemHttpResult>`)
- `IEndpointGroup` auto-discovery pattern
- CQRS with MediatR + pipeline behaviors (validation, logging, performance)
- Result pattern with `Result<T>` / `Error` for error propagation
- JWT auth with role-based authorization policies
- Security headers middleware
- Proper `CancellationToken` usage throughout
- Feature-based folder organization in Application layer

### Issues Found (Severity: Critical → Minor)

#### Critical
1. **No Rate Limiting** — Auth endpoints (`/login`, `/register`, `/refresh`) are unprotected against brute-force/credential-stuffing attacks. .NET 7+ has built-in `RateLimiter` middleware.
2. **No Health Checks** — CLAUDE.md documents `GET /health` but no implementation exists. Required for container orchestration (Docker/K8s liveness/readiness probes).
3. **No CORS Configuration** — API is consumed by frontend but has no CORS policy. Will block browser requests.

#### High
4. **Custom middleware instead of built-in .NET 8 mechanisms** — `ExceptionHandlerMiddleware` can be replaced with `IExceptionHandler` + `AddProblemDetails()`. `SecurityHeadersMiddleware` can be replaced with `HeaderPolicyCollection` from `NetEscapades.AspNetCore.SecurityHeaders`. The custom middleware doesn't integrate with .NET 8's ProblemDetails infrastructure.
5. **No API Versioning** — No strategy for evolving the API without breaking clients. Once in production, changing endpoints becomes very painful.
6. **Serilog not enriched** — No request ID / correlation ID, no structured enrichment. In production with multiple instances, you can't trace a request through logs.
7. **Swashbuckle (Swagger) is deprecated** — Microsoft announced Swashbuckle won't be updated. Should migrate to **Scalar** for .NET 8+ (modern, actively maintained, better DX).

#### Medium
8. **Namespace inconsistency** — `AuthEndpoints` uses `OrderHub.Api.Endpoints.Auth` but `ProductEndpoints` uses `OrderHub.Api.Features.Products`. Physical files are all under `Endpoints/` but namespaces don't match. This causes confusion and potential DI/scan issues.
9. **DB migration in Program.cs** — `dbContext.Database.MigrateAsync()` + `DataSeeder.Seed()` inline in Program.cs is fragile. Should use `IHostedService` or `StartupFilter` for graceful error handling and logging.
10. **No Request Timeout** — No global or per-endpoint timeout configuration. A hung DB query can block a thread indefinitely.
11. **No Output Caching** — Product listing, product detail are cacheable GET endpoints but have no cache headers or output caching.
12. **Missing response compression** — No response compression for JSON payloads.

#### Low
13. **No `ForwardedHeaders` configuration** — Will break when deployed behind a reverse proxy (Nginx, load balancer). `Request.Host` and `Request.Scheme` will be wrong.
14. **JWT Key in plain appsettings** — Development only has the key, but production config should use env vars or secret manager. The key should have minimum length validation at startup (already has `JwtOptionsValidator` but worth verifying).
15. **No OpenAPI operation IDs / examples** — Endpoint names are set but more descriptive operation IDs and example payloads would improve generated client code quality.
16. **Logout endpoint allows anonymous** — Should require authentication to prevent token abuse.

---

## Implementation Plan

### Phase 1: Security & Infrastructure (Critical)

**Step 1** — Add Rate Limiting
- **Files:** `Api/Common/RateLimitingExtensions.cs`, `Api/DependencyInjection.cs`, `Api/Program.cs`
- **Approach:** Use built-in `System.Threading.RateLimiting` with fixed-window or sliding-window limiter. Apply strict limits on auth endpoints, generous on read endpoints.
- **NuGet:** No additional — built-in since .NET 7

**Step 2** — Add Health Checks
- **Files:** `Api/Common/HealthCheckExtensions.cs`, `Api/DependencyInjection.cs`
- **Approach:** Use `AspNetCore.HealthChecks` with PostgreSQL check. Expose `/health` (liveness) and `/health/ready` (readiness including DB).
- **NuGet:** `AspNetCore.HealthChecks.NpgSql`

**Step 3** — Add CORS Configuration
- **Files:** `Api/DependencyInjection.cs`
- **Approach:** Configure named CORS policy with configurable origins from appsettings. Default to restrictive, allow override per environment.

### Phase 2: Modernize Middleware & Error Handling (High)

**Step 4** — Replace ExceptionHandlerMiddleware with IExceptionHandler + ProblemDetails
- **Files:** Delete `Api/Middleware/ExceptionHandlerMiddleware.cs`, create `Api/ExceptionHandler/GlobalExceptionHandler.cs`
- **Approach:** Use `IExceptionHandler` interface + `builder.Services.AddProblemDetails()`. This integrates properly with .NET 8's built-in ProblemDetails infrastructure and minimal APIs.
- **Why:** Custom middleware can't properly integrate with `TypedResults.Problem()` and the ProblemDetails service. The built-in approach handles content negotiation, trace IDs, and RFC 7807 compliance automatically.

**Step 5** — Replace SecurityHeadersMiddleware with NetEscapades.AspNetCore.SecurityHeaders
- **Files:** Delete `Api/Middleware/SecurityHeadersMiddleware.cs`, update `Api/DependencyInjection.cs`
- **NuGet:** `NetEscapades.AspNetCore.SecurityHeaders`
- **Approach:** Use `HeaderPolicyCollection` with fluent API. More comprehensive headers (Strict-Transport-Security with max-age, permissions-policy, etc.) and easier to maintain.

**Step 6** — Add API Versioning
- **Files:** `Api/DependencyInjection.cs`, update all endpoint groups
- **NuGet:** `Asp.Versioning.Http`, `Asp.Versioning.Mvc`
- **Approach:** URL-segment versioning (`/api/v1/products`). Set default version to 1.0. Add `IApiVersionDescriptionProvider` for OpenAPI per version.

### Phase 3: Observability & DX (High → Medium)

**Step 7** — Migrate from Swashbuckle to Scalar
- **Files:** `Api/DependencyInjection.cs`, remove Swashbuckle NuGet, add Scalar NuGet
- **NuGet:** `Scalar.AspNetCore` (replace `Swashbuckle.AspNetCore`)
- **Why:** Swashbuckle is deprecated. Scalar is actively maintained, generates beautiful docs, and has built-in client code generation.

**Step 8** — Enhance Serilog with enrichment
- **Files:** `Api/DependencyInjection.cs`, `appsettings.json`
- **NuGet:** `Serilog.Enrichers.Span` (correlation via Activity), `Serilog.Enrichers.Environment` (machine name), `Serilog.Expressions` or `Serilog.Sinks.Console` with formatter
- **Approach:** Add `Enrich.FromLogContext()`, `Enrich.WithMachineName()`, `Enrich.WithEnvironmentName()`. Use structured output template with `{SourceContext}` and `{RequestId}`.

**Step 9** — Fix namespace inconsistency
- **Files:** Move/rename namespaces in `ProductEndpoints.cs`, `CreateProductRequest.cs`, `UpdateProductRequest.cs`
- **Approach:** Unify to `OrderHub.Api.Endpoints.{Feature}` pattern. Physical folders already match, only namespaces are wrong.

### Phase 4: Robustness & Performance (Medium)

**Step 10** — Extract DB migration to hosted service
- **Files:** Create `Api/Infrastructure/DatabaseMigrationHostedService.cs`
- **Approach:** Implement `IHostedService` with retry logic, proper logging, and graceful failure. Remove inline migration from Program.cs.

**Step 11** — Add Request Timeout + Output Caching
- **Files:** `Api/DependencyInjection.cs`, endpoint groups
- **NuGet:** Built-in `RequestTimeouts` middleware, built-in `OutputCache`
- **Approach:** Global 30s timeout. Output cache on GET products (5 min sliding, invalidate on write).

**Step 12** — Add ForwardedHeaders + Response Compression
- **Files:** `Api/DependencyInjection.cs`
- **Approach:** `app.UseForwardedHeaders()` before other middleware. `services.AddResponseCompression()` with Brotli/Gzip providers.

---

## Recommended NuGet Packages Summary

| Package | Purpose | Priority |
|---------|---------|----------|
| `Scalar.AspNetCore` | Replace deprecated Swashbuckle | High |
| `AspNetCore.HealthChecks.NpgSql` | Health check with PostgreSQL probe | Critical |
| `NetEscapades.AspNetCore.SecurityHeaders` | Replace custom security headers middleware | High |
| `Asp.Versioning.Http` | API versioning (URL segment) | High |
| `Serilog.Enrichers.Span` | Correlation ID via Activity | High |
| `Serilog.Enrichers.Environment` | Machine name + env in logs | Medium |

> Note: Rate limiting, request timeouts, output caching, and response compression are all built-in since .NET 7/8 — no additional NuGet needed.

---

## Open Questions

1. **Rate limit strategy:** Fixed-window (simpler) or sliding-window (smoother)? Recommend fixed-window for MVP.
2. **API versioning format:** URL segment (`/api/v1/products`) or header-based? Recommend URL segment for simplicity and discoverability.
3. **CORS origins:** Which origins should be allowed in production? Need actual frontend URL.
4. **Serilog sinks:** Console only for now, or should we configure Seq/Elasticsearch/Application Insights for production?

## Risks

- **Breaking change:** API versioning adds `/v1/` to all routes — existing clients need updating. Mitigate by setting default version so unversioned routes still work.
- **Scalar migration:** Scalar's OpenAPI integration differs from Swashbuckle — some Swagger-specific annotations may need updating.
- **Output caching:** Must ensure cache invalidation on product create/update/delete. Event-driven invalidation already exists in Infrastructure but needs wiring to output cache tags.
