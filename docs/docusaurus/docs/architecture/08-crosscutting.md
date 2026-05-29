---
sidebar_position: 8
title: "8. Cross-cutting Concepts"
description: Patterns and concerns that span multiple architectural layers
---

# 8. Cross-cutting Concepts

## 8.1 Error Handling

OrderHub uses a dual-path error handling strategy that always produces RFC 9457 ProblemDetails:

| Path | Layer | Mechanism | Response |
|------|-------|-----------|----------|
| **Business errors** | Application | Handler returns `Result<T>.Failure(Error)` | Mapped by `ResultExtensions` → 4xx ProblemDetails |
| **Validation errors** | Application | `ValidationBehavior` catches FluentValidation failures | 400 ProblemDetails with field-level details |
| **Unexpected errors** | API | `GlobalExceptionHandler` middleware catches exceptions | 500 ProblemDetails, no stack trace leak |

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["'Email' must be a valid email address."],
    "Password": ["Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character."]
  }
}
```

## 8.2 Validation

FluentValidation is integrated via MediatR pipeline behavior:

- **ValidationBehavior** automatically validates all commands/queries before reaching the handler
- Validators are co-located with their features (e.g., `CreateProductCommandValidator`)
- Validation rules cover: required fields, string lengths, email format, password complexity, numeric ranges, SKU uniqueness
- Validation failures short-circuit the pipeline — the handler is never called

## 8.3 Logging and Observability

### Serilog Configuration

| Sink | Environments | Format | Retention |
|------|-------------|--------|-----------|
| **Console** | All | Structured text | N/A |
| **File** | All | Rolling JSON | 100MB/file, 14 days |
| **Seq** | Dev only | Structured events | Ephemeral |

### Enrichers

| Enricher | Provides |
|----------|----------|
| FromLogContext | Request-scoped properties |
| MachineName | Server identity |
| EnvironmentName | Dev/Staging/Production |
| ProcessId | Process identifier |
| ThreadId | Thread identifier |
| Serilog.Enrichers.Span | TraceId/SpanId (OpenTelemetry ready) |
| ExceptionDetails | Destructured exception information |

### Sensitive Data Protection

- **SensitiveDataDestructuringPolicy** — Redacts JWT tokens and PII before serialization
- **SensitiveLogEventFilter** — Filters sensitive properties before writing to any sink
- No password, token, or PII appears in log output

## 8.4 Security

### Authentication & Authorization

| Concern | Implementation |
|---------|---------------|
| Token format | JWT (HS256) |
| Access token lifetime | 15 minutes |
| Refresh token lifetime | 7 days |
| Password hashing | `PasswordHasher<User>` (PBKDF2, HMAC-SHA256) |
| Authorization | Role-based (Admin, Customer) + custom policies (owner-or-admin) |

### Rate Limiting

Per-endpoint sliding window rate limiting, partitioned by user ID (authenticated) or IP (anonymous):

| Endpoint Group | Limit | Partition |
|---------------|-------|-----------|
| Login | 5 req/min | IP address |
| Register | 3 req/min | IP address |
| Refresh | 10 req/min | IP address |
| Products | 60 req/min | User ID / IP |
| Orders | 30 req/min | User ID / IP |
| Admin | 40 req/min | User ID / IP |

### Input Sanitization

- **HtmlInputSanitizer** (HtmlSanitizer v9.0.892) strips all HTML from string inputs
- **SanitizeHtmlEndpointFilter** auto-sanitizes all string properties on request DTOs via reflection
- Applied to Auth and Products endpoints
- Prevents stored XSS attacks

### Security Headers

All responses include: HSTS, X-Content-Type-Options, X-Frame-Options, Content-Security-Policy (via NetEscapades.AspNetCore.SecurityHeaders).

## 8.5 Caching

### Strategy: IMemoryCache with Version-Key Pattern

Handler-level caching with atomic version-based invalidation:

```
CacheKey = "{prefix}:v{version}:{params}"
```

**How it works:**
1. On read → generate cache key with current version → check IMemoryCache
2. On cache hit → return cached result
3. On cache miss → query database → cache with TTL → return
4. On mutation → reset version → old cache entries become orphaned → expire by TTL

### Cache Policies

| Data | TTL | Invalidation |
|------|-----|-------------|
| Product list | Sliding 30s, Absolute 5min | Version reset on any product mutation |
| Product by ID | Sliding 30s, Absolute 10min | Version reset on product update/delete |
| Top products report | Absolute 3min | Version reset on order/product mutation |
| Revenue by day report | Absolute 3min | Version reset on order/product mutation |

## 8.6 Response Compression

Brotli + Gzip compression on all HTTP responses. Configured in DI with optimal compression levels.

## 8.7 API Versioning

URL segment versioning via `Asp.Versioning`:

```
/api/v1/auth/...
/api/v1/products/...
/api/v1/orders/...
```

Currently only v1 is implemented. The versioning infrastructure is in place for future major versions.
