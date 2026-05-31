---
sidebar_position: 8
title: "8. Cross-cutting Concepts"
description: Patterns and concerns that span multiple architectural layers
---

# 8. Cross-cutting Concepts

This section describes cross-cutting concerns that span multiple layers of the OrderHub architecture.

## 8.1 Error Handling & ProblemDetails Mapping

OrderHub implements a unified error-handling strategy designed to prevent unhandled database exceptions from leaking to clients while ensuring business failures return predictable, structured responses compliant with the **RFC 9457 ProblemDetails** standard.

The application divides error handling into two distinct execution tracks:

```
[Request Execution]
       |
       +---> Business Failure? (e.g., Stock Insufficient)
       |     - Handlers return Result.Failure(Error)
       |     - Mapped by ResultExtensions to HTTP 400/404/409
       |     - Output: RFC 9457 ProblemDetails JSON
       |
       +---> Validation or Unhandled Infrastructure Exception?
             - Handlers throw Exception / FluentValidation fails
             - Caught by GlobalExceptionHandler Middleware
             - Output: RFC 9457 ProblemDetails JSON (Stack trace redacted)
```

### 8.1.1 Format of a Validation Problem Details Response
When FluentValidation constraints fail inside the `ValidationBehavior` pipeline, the endpoint returns an HTTP 400 response containing field-level validation details:

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

---

## 8.2 Validation Pipeline

Input validation is enforced at the Application layer boundary using **FluentValidation** integrated via a MediatR pipeline behavior (`ValidationBehavior`).

*   **Automatic Interception:** Before any request handler is executed, the pipeline behavior retrieves all registered validators (`AbstractValidator<T>`) for the incoming Command or Query.
*   **Fail-Fast Execution:** If any validation rules fail, execution is short-circuited. The pipeline prevents the request from reaching the handler, ensuring business logic operates only on valid input.
*   **Separation of Concerns:** Validators are co-located with their corresponding CQRS features (e.g., `CreateProductCommandValidator` is defined inside the `/Features/Products/CreateProduct` directory). This keeps validation rules close to the command schemas they validate.

---

## 8.3 Logging, Observability, and Telemetry Redaction

OrderHub uses **Serilog** for structured logging, outputting events as machine-readable JSON payloads that can be indexed by log aggregators.

### 8.3.1 Structured Logging Sinks

| Logging Sink | Environments | Log Format | Retention & Roll-over Configuration |
|---|---|---|---|
| **Console Sink** | Development / Staging | Structured plain text with color | N/A |
| **File Sink** | Staging / Production | Structured JSON | Roll-over per 100MB; retains a rolling window of 14 days. |
| **Seq Sink** | Local Development | HTTP POST ingestion | Ephemeral (used for local query analysis and debugging). |

### 8.3.2 Tracing & Correlation ID Propagation
Every incoming HTTP request is assigned a unique identifier via the `CorrelationIdMiddleware`:
1.  The middleware extracts `X-Correlation-ID` from the incoming HTTP request headers. If absent, it generates a new GUID.
2.  The correlation ID is attached to the ASP.NET Core request context and written to the HTTP response headers.
3.  It is pushed to Serilog's `LogContext`, appending the `CorrelationId` property to every log entry generated during the lifecycle of the request.
4.  Logs also capture `TraceId` and `SpanId` (via `Serilog.Enrichers.Span`), ensuring compatibility with distributed tracing collectors like OpenTelemetry and Jaeger.

### 8.3.3 Sensitive Data Protection (Logging Redaction)
To prevent Personally Identifiable Information (PII) or credentials from leaking into log storage, the infrastructure layer implements a strict redaction policy:
*   `SensitiveDataDestructuringPolicy`: An EF Core entity and DTO destructuring interceptor. When log entries include user objects, properties like `PasswordHash`, `Token`, `Email`, and `FullName` are redacted.
*   `SensitiveLogEventFilter`: A log filter that scans log parameters before writing to sinks, masking properties containing passwords, tokens, or security headers.

---

## 8.4 Security and Input Sanitization

Security controls are applied at multiple layers to protect the application from common web vulnerabilities.

```
                  +---------------------------------------+
                  |           Client HTTP Request         |
                  +-------------------+-------------------+
                                      |
                                      v
                  +-------------------+-------------------+
                  |        Security Response Headers      |
                  |     (CSP, HSTS, X-Frame-Options)      |
                  +-------------------+-------------------+
                                      |
                                      v
                  +-------------------+-------------------+
                  |         Rate Limiting Middleware      |
                  |     (Sliding Window by User or IP)    |
                  +-------------------+-------------------+
                                      |
                                      v
                  +-------------------+-------------------+
                  |        SanitizeHtmlEndpointFilter     |
                  |      (Strips malicious HTML script)   |
                  +---------------------------------------+
```

### 8.4.1 Authentication & Authorization Rules
*   **JWT Authentication:** Access tokens are signed using HMAC-SHA256 (HS256) and expire in 15 minutes. Long-lived refresh tokens (7 days) are tracked in the database to rotate access tokens without forcing users to re-login.
*   **Cryptographic Password Hashing:** User passwords are encrypted using ASP.NET Core's `PasswordHasher<User>`, which utilizes the PBKDF2 (Password-Based Key Derivation Function 2) algorithm with HMAC-SHA256 and a dynamic salt.
*   **Endpoint Security Policies:** Endpoints are protected with role-based policies (e.g., `Admin` only for reports). A custom filter verifies resource ownership (e.g., ensuring a customer can only retrieve or cancel their own orders).

### 8.4.2 Sliding-Window Rate Limiting
API endpoints are protected from denial-of-service (DoS) and brute-force attacks via sliding-window rate limiters partitioned by User ID (for authenticated users) or IP Address (for anonymous users):

| Route / Endpoint | Rate Limit | Window Size | Partitioning Key |
|---|---|---|---|
| `/api/v1/auth/login` | 5 requests | 1 minute | Client IP Address |
| `/api/v1/auth/register` | 3 requests | 1 minute | Client IP Address |
| `/api/v1/auth/refresh` | 10 requests | 1 minute | Client IP Address |
| `/api/v1/products` (List/Detail) | 60 requests | 1 minute | User ID / Client IP |
| `/api/v1/orders` | 30 requests | 1 minute | User ID / Client IP |
| `/api/v1/admin/reports` | 40 requests | 1 minute | User ID / Client IP |

### 8.4.3 HTML Sanitization (Stored XSS Prevention)
To prevent Cross-Site Scripting (XSS) attacks, OrderHub sanitizes text inputs at the API boundary before they are written to the database.
*   The `SanitizeHtmlEndpointFilter` is applied to endpoints that accept user-provided strings (e.g., Product creation, User registration).
*   The filter scans incoming DTOs using reflection to identify all string properties.
*   It passes text through `HtmlSanitizer` (v9.0.892), stripping malicious tags (like `<script>`, `<iframe>`, `<object>`) and event attributes (like `onload`, `onclick`).

### 8.4.4 Security Headers
All HTTP responses include security-hardening headers configured via `NetEscapades.AspNetCore.SecurityHeaders`:
*   `X-Frame-Options: DENY` (prevents clickjacking).
*   `X-Content-Type-Options: nosniff` (prevents MIME sniffing).
*   `X-XSS-Protection: 0` (disables legacy, vulnerable browser filters).
*   `Content-Security-Policy: default-src 'self'` (limits content sources).
*   `Strict-Transport-Security` (enforces HTTPS connections).

---

## 8.5 Caching and Cache Stampede Protection

To maintain low API latency and protect database resources, OrderHub implements a caching strategy using `IMemoryCache` combined with a custom version-key invalidation pattern and cache stampede protection.

### 8.5.1 The Version-Key Invalidation Pattern
Since standard in-memory caches do not support tag-based eviction, OrderHub uses a version-key pattern to invalidate cached list queries:

1.  **Cache Key Format:** Cache keys for paginated lists incorporate a version token:
    ```
    products:list:v{version}:{page}:{pageSize}:{filters}
    ```
2.  **Version Lookup:** Before querying the cache, the application checks `IMemoryCache` for the current version token (`products:version`). If absent, a new random 8-character version token is generated.
3.  **Invalidation Trigger:** When a mutation occurs (e.g., a product is updated, deleted, or a new order is created), the application removes the version token (`products:version`) from the cache.
4.  **Result:** The next query generates a new version token, causing subsequent requests to bypass old cached entries. The orphaned cache entries are left to expire naturally by their TTL (Time-To-Live), preventing stale data from being served.

### 8.5.2 Cache Stampede Guard
When a high-traffic cache entry expires, multiple concurrent requests can attempt to query the database simultaneously (the **Thundering Herd** problem).

OrderHub prevents this using a thread-safe `CacheStampedeGuard`:
*   If a cache miss occurs, the application retrieves or creates a `SemaphoreSlim(1, 1)` mapped to that cache key from a concurrent dictionary.
*   The requesting thread waits on the semaphore, serializing database queries for that key.
*   Once the lock is acquired, the thread double-checks the cache. If another thread already fetched the data and populated the cache while this thread was waiting, it returns the cached data immediately.
*   If it is still a cache miss, the thread queries the database, writes the result to the cache, and releases the lock.

---

## 8.6 Database Search Optimization (GIN Trigram Indexing)

Product catalog searches allow users to search products by name using case-insensitive substring matches (e.g., `WHERE Name ILIKE '%search%'`).

By default, standard B-tree database indexes cannot optimize substring queries containing wildcard prefixes (e.g., `%search%`), forcing the database engine to perform slow, full-table sequence scans.

OrderHub optimizes these searches using **GIN Trigram Indexing**:
*   **Database Extension:** The `pg_trgm` extension is registered in `OrderHubDbContext.cs` to split text columns into 3-character sequences (trigrams).
*   **Index Configuration:** In `ProductConfiguration.cs`, a GIN (Generalized Inverted Index) index is configured on the `Name` column with trigram operators:
    ```csharp
    builder.HasIndex(p => p.Name)
        .HasMethod("gin")
        .HasOperators("gin_trgm_ops")
        .HasDatabaseName("IX_Products_Name_Trgm");
    ```
*   **Query Implementation:** The `ProductRepository` uses `EF.Functions.ILike(p.Name, $"%{search}%")` to compile database queries into SQL `ILIKE` statements, leveraging the GIN index for faster search execution.

---

## 8.7 Response Compression

To optimize network bandwidth usage, OrderHub applies response compression to all API responses using Brotli and Gzip compression algorithms:
*   **Brotli Compression:** Used as the primary compression provider for browsers and clients supporting it, configured with the `Optimal` compression level.
*   **Gzip Compression:** Fallback compression provider for clients that do not support Brotli.

---

## 8.8 API Versioning

API endpoints are versioned using URL segments (e.g., `/api/v1/...`) configured via `Asp.Versioning`:
*   Allows the API to expose new versions of endpoints while maintaining backward compatibility for existing clients.
*   Configured to automatically read versions from URL segments and format error details using the ProblemDetails format.
