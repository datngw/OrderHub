---
sidebar_position: 10
title: "ADR-009: Result Pattern over Exceptions"
description: Decision to use Result<T> return type instead of exceptions for business error handling
---

# ADR-009: Result Pattern over Exceptions

## Status

✅ Accepted

## Context

Business operations can fail in expected ways: product not found, insufficient stock, invalid credentials, order already cancelled. These are not exceptional conditions — they are normal business outcomes that must be communicated to the caller. The error handling strategy must be explicit, type-safe, and produce consistent RFC 9457 ProblemDetails responses.

## Decision

Handlers return **`Result<T>`** instead of throwing exceptions for expected business failures. Typed error collections (`ProductErrors`, `AuthErrors`, `OrderErrors`) define domain-specific errors. `ResultExtensions` maps `Result<T>` to HTTP responses with RFC 9457 ProblemDetails.

## Rationale

Three error handling strategies were considered:

| Criteria | Result\<T\> pattern | Exception-based | OneOf discriminated unions |
|----------|--------------------|-----------------|---------------------------|
| **Explicit handling** | ✅ Caller must handle failure | ❌ Easy to miss (no catch) | ✅ Compiler-enforced |
| **Performance** | ✅ No stack trace overhead | ❌ Stack trace capture is expensive | ✅ Value type, no overhead |
| **Typed errors** | ✅ Static error classes per domain | ❌ Exception hierarchy | ✅ Per-return-type |
| **HTTP mapping** | ✅ `ResultExtensions` → ProblemDetails | ⚠️ Exception middleware | ⚠️ Custom mapping needed |
| **Conventions** | C# idiomatic (growing pattern) | C# historical default | Functional style (less common in C#) |

The Result pattern was chosen because:

1. **Explicit error handling** — the return type forces callers to acknowledge and handle failure cases; errors cannot be silently ignored
2. **No stack trace overhead** — business failures don't capture expensive stack traces; this matters under load where "product not found" is common
3. **Typed errors** — static classes like `ProductErrors.NotFound`, `AuthErrors.InvalidCredentials` provide compile-time discoverable error definitions
4. **Consistent HTTP mapping** — `ResultExtensions` converts any `Result<T>` to the appropriate HTTP response with RFC 9457 ProblemDetails

Exception-based handling was rejected because exceptions for expected business failures conflate control flow with error handling and add stack trace overhead. OneOf was rejected because it's a less common pattern in C# and would require additional mapping to ProblemDetails.

**Note:** Exceptions are still used for truly unexpected failures (unhandled errors), caught by `GlobalExceptionHandler` and converted to 500 ProblemDetails.

## Consequences

**Positive:**
- Explicit error handling — caller must deal with failure cases
- No stack trace overhead for business errors
- Typed errors via static classes (`ProductErrors`, `AuthErrors`, `OrderErrors`)
- Both business and exception paths converge to RFC 9457 ProblemDetails at the API layer
- Easy to unit test — handlers return deterministic `Result<T>` values

**Negative:**
- More verbose than throwing exceptions (explicit `Result<T>` handling at every call site)
- Developers must remember to use `Result<T>` instead of throwing (mitigated by code review)
- No compiler enforcement to handle the failure case (unlike OneOf/F#)
