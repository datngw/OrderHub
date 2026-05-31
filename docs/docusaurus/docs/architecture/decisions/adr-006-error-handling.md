---
sidebar_position: 7
title: "ADR-006: Result Pattern and RFC 9457 ProblemDetails Error Handling"
description: Decision to use the Result pattern for business logic errors and the RFC 9457 ProblemDetails standard for HTTP responses
---

# ADR-006: Result Pattern and RFC 9457 ProblemDetails Error Handling

## Status

✅ Accepted

## Context

HTTP client applications require a standardized, predictable format to process API errors. In early API implementations, error handling faced several issues:
*   Unhandled database or infrastructure exceptions leaked stack traces and internal schemas to clients, creating security risks.
*   Validation errors returned different JSON schemas than business errors.
*   Expected failures (e.g., product not found, insufficient stock) threw exceptions, causing performance overhead due to stack trace generation.

The system needs a unified error-handling strategy that prevents raw exception leakage, avoids exception overhead for business errors, and provides structured, machine-readable HTTP error responses.

## Decision

Adopt the **Result Pattern** (`Result<T>`) for business logic errors in the Application layer, and standardise HTTP error responses using the **RFC 9457 ProblemDetails** standard.

### 1. Result Pattern (Business Logic Errors)
Handlers return a `Result<T>` or `Result` container indicating success or failure. Failures encapsulate an `Error` record (code and description). Business exceptions are prohibited for expected flows:
```csharp
public static class ProductErrors
{
    public static Error NotFoundById(Guid id) => 
        new("Product.NotFoundById", $"The product with ID '{id}' was not found.");
}
```

### 2. RFC 9457 ProblemDetails (API Boundary)
At the API boundary, `ResultExtensions` maps failures to corresponding HTTP status codes (400, 404, 409). Unhandled infrastructure exceptions are caught by `GlobalExceptionHandler` middleware, logged structured, and returned as generic HTTP 500 ProblemDetails (redacting stack traces in non-development environments).

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["'Email' must be a valid email address."]
  }
}
```

## Rationale

Three error handling strategies were evaluated:

| Attribute | Result Pattern + ProblemDetails | Exception-based Flow | Custom Envelope DTO |
|---|---|---|---|
| **Performance Overhead** | ✅ Low (No stack trace generation) | ❌ High (Exceptions generate stack traces) | ✅ Low |
| **Separation of Concerns** | ✅ Decoupled (API maps results to HTTP) | ❌ Poor (Exceptions bubble up from domain) | ✅ Decoupled |
| **API Standardization** | ✅ High (RFC 9457 Industry Standard) | ❌ Low (ad-hoc shapes) | ⚠️ Moderate (Custom schema) |
| **Type Safety** | ✅ High (Handler return types are explicit) | ❌ Low (Exceptions are implicit) | ✅ High |

This strategy was chosen because:
1.  **Improves Performance:** Avoids the CPU and memory overhead of generating stack traces for expected business failures (e.g., incorrect passwords, out-of-stock items).
2.  **Standardizes Errors:** Ensures that validation errors, business failures, and system exceptions return a consistent JSON schema, simplifying client-side error parsing.
3.  **Prevents Data Leakage:** The `GlobalExceptionHandler` middleware automatically redacts internal stack traces and database schemas in production.
4.  **Improves Type Safety:** Handlers explicitly return `Result<T>`, forcing developers to handle both success and failure cases.

## Consequences

**Positive:**
*   Eliminates exception overhead for expected business logic errors.
*   Enforces a clean separation of concerns, keeping the Application layer independent of HTTP concerns.
*   Protects database and system details from leaking to clients.
*   Provides a standardized, predictable error contract for client integrations.

**Negative:**
*   **Boilerplate:** Requires handlers to wrap return values in `Result<T>` and define static `Error` records.
*   **Mapping Overhead:** Requires maintaining status code mapping logic inside `ResultExtensions` and `GlobalExceptionHandler`.
