---
sidebar_position: 10
title: "10. Quality Requirements"
description: Quality attribute requirements covering performance, reliability, security, and testability
---

# 10. Quality Requirements

## 10.1 Performance

| Requirement | Target | Mechanism |
|------------|--------|-----------|
| Database connection pooling | Min 5 / Max 100 connections | Npgsql pooling configuration |
| Read query optimization | No entity tracking overhead | `AsNoTracking()` on all read queries |
| Complex query optimization | N+1 prevention | `AsSplitQuery()` on Order with Items |
| Response size | Minimize bandwidth | Brotli + Gzip compression |
| Database indexes | Cover all query patterns | Indexes on Orders, OrderItems, Products |
| Report caching | Reduce DB load | IMemoryCache with 3-minute absolute expiry |
| Product list caching | Reduce DB load | IMemoryCache with 30s sliding / 5min absolute |

## 10.2 Reliability

| Requirement | Target | Mechanism |
|------------|--------|-----------|
| No overselling | Zero violations under concurrency | Pessimistic locking (`SELECT ... FOR UPDATE`) |
| Transaction atomicity | All-or-nothing order creation | EF Core transactions via UnitOfWork |
| Database resilience | Auto-retry on transient failures | `EnableRetryOnFailure()` |
| Health monitoring | Liveness + Readiness probes | `/health/live` (process) + `/health/ready` (DB check) |
| Graceful degradation | ProblemDetails on all errors | RFC 9457 compliant error responses |

### Concurrency Verification

The pessimistic locking strategy is verified with integration tests:

- **Test**: 20 concurrent HTTP requests to create an order for the same product with stock=5
- **Expected**: Exactly 5 requests succeed (201), 15 fail with insufficient stock (409)
- **Final state**: Product stock = 0
- **Verified with**: Testcontainers (real PostgreSQL), not mocks

## 10.3 Security

| Requirement | Implementation | Verified |
|------------|---------------|---------|
| Authentication | JWT access (15min) + refresh (7 days) | ✅ Unit + integration tests |
| Password hashing | PBKDF2 with HMAC-SHA256 | ✅ Auto-upgradable format |
| Password complexity | Uppercase + lowercase + digit + special char | ✅ FluentValidation |
| JWT key strength | Minimum 32 characters | ✅ Startup validation |
| Rate limiting | Per-endpoint, partitioned by userId/IP | ✅ Sliding window |
| XSS prevention | HtmlSanitizer strips all HTML | ✅ 12 unit tests |
| Security headers | HSTS, CSP, X-Frame-Options, X-Content-Type-Options | ✅ NetEscapades |
| No stack trace leak | GlobalExceptionHandler returns no stack traces | ✅ ProblemDetails only |
| No entity exposure | Separate request/response DTOs | ✅ Architectural convention |
| Sensitive data in logs | Redacted by destructuring policy + filter | ✅ Serilog config |

## 10.4 Testability

| Requirement | Target | Actual |
|------------|--------|--------|
| Application layer coverage | ≥ 60% | ✅ 52 unit tests (handlers + validators) |
| Integration tests | Critical paths covered | ✅ Concurrency + performance tests |
| Test isolation | No cross-test contamination | ✅ WebApplicationFactory + Testcontainers |
| Mocking strategy | Interfaces only | ✅ Moq on repository/service interfaces |

### Test Distribution

| Category | Tests | What They Cover |
|----------|-------|-----------------|
| Auth handlers | 4 | Register, Login, Refresh, Logout |
| Auth validators | 4 | Email format, password complexity, required fields |
| Product handlers | 5 | CRUD operations + list with filtering |
| Product validators | 2 | Create + Update validation rules |
| Order handlers | 5 | Create, Cancel, UpdateStatus, GetById, GetMyOrders |
| Order validators | 2 | Create + UpdateStatus validation |
| Report handlers | 2 | TopProducts, RevenueByDay |
| Report tests | 1 | Validator + cache invalidation |
| HTML sanitization | 12 | Script injection, iframe, event handlers, SVG XSS, mixed content |
| Integration | ~10 | Concurrency, performance, query correctness |

## 10.5 Maintainability

| Requirement | Implementation |
|------------|---------------|
| Layered architecture | Strict 4-layer separation with dependency inversion |
| Thin endpoints | HTTP concerns only in API layer |
| CQRS pattern | Clear separation of read/write paths |
| Explicit errors | `Result<T>` with typed error codes |
| Centralized caching | `CacheKeys` static class |
| Consistent validation | FluentValidation via pipeline behavior |
