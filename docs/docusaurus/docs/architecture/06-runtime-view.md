---
sidebar_position: 6
title: "6. Runtime View"
description: Important runtime scenarios showing how components interact at runtime
---

# 6. Runtime View

## 6.1 Scenario: Create Order (Most Critical Path)

The order creation flow is the most complex scenario — it involves authentication, validation, pessimistic locking, atomic stock deduction, price snapshotting, and cache invalidation within a single transaction.

```mermaid
sequenceDiagram
    participant C as Customer
    participant MW as Auth Middleware
    participant E as POST /orders
    participant PL as MediatR Pipeline
    participant VB as ValidationBehavior
    participant H as CreateOrderHandler
    participant DB as PostgreSQL
    participant Cache as IMemoryCache

    C->>MW: POST /api/v1/orders<br/>Authorization: Bearer {jwt}
    MW->>MW: Validate JWT + Extract UserId/Role
    MW->>E: Authenticated Request
    E->>PL: CreateOrderCommand<br/>(UserId, Items[])
    PL->>VB: Validate Command
    VB->>VB: Quantity > 0, Items non-empty
    VB->>H: Validated Command
    H->>DB: BEGIN TRANSACTION
    H->>DB: SELECT * FROM Products<br/>WHERE Id IN (...)<br/>FOR UPDATE
    Note over DB: Row-level lock acquired<br/>on all requested products
    H->>H: Validate stock availability
    alt Stock insufficient
        H->>DB: ROLLBACK
        H-->>PL: Result.Failure(InsufficientStock)
    end
    H->>H: Calculate TotalAmount<br/>from current product prices
    H->>DB: INSERT Order
    H->>DB: INSERT OrderItems<br/>(with UnitPrice snapshot)
    H->>DB: UPDATE Products<br/>SET Stock = Stock - @Quantity
    H->>DB: COMMIT
    Note over DB: Lock released on commit
    H->>Cache: InvalidateProductsVersion()<br/>InvalidateReportsVersion()
    H-->>PL: Result.Success(OrderResponse)
    PL-->>E: Result<OrderResponse>
    E-->>C: 201 Created<br/>{ order with items }
```

### Key Guarantees

| Guarantee | Mechanism |
|-----------|-----------|
| No overselling | `SELECT ... FOR UPDATE` locks product rows until transaction commits |
| Price accuracy | `UnitPrice` is snapshotted from `Product.Price` at order creation time |
| Atomicity | Stock deduction + order creation in a single database transaction |
| Consistency | If any step fails, the entire transaction rolls back |
| Cache coherence | Product and report cache versions are invalidated after successful order |

## 6.2 Scenario: Authentication (Register → Login)

```mermaid
sequenceDiagram
    participant C as Client
    participant E as Auth Endpoint
    participant PL as MediatR Pipeline
    participant H as Auth Handler
    participant DB as PostgreSQL

    Note over C,DB: Registration
    C->>E: POST /api/v1/auth/register
    E->>PL: RegisterCommand
    PL->>H: Validated Command
    H->>DB: Check email uniqueness
    H->>H: Hash password (PasswordHasher<T>)
    H->>DB: INSERT User (Role=Customer)
    H-->>C: 201 Created { userId }

    Note over C,DB: Login
    C->>E: POST /api/v1/auth/login
    E->>PL: LoginCommand
    PL->>H: Validated Command
    H->>DB: Find user by email
    H->>H: Verify password hash
    H->>H: Generate JWT access token (15 min)
    H->>H: Generate refresh token (7 days)
    H->>DB: INSERT RefreshToken
    H-->>C: 200 OK { accessToken, refreshToken }
```

## 6.3 Scenario: Cached Report Query

```mermaid
sequenceDiagram
    participant A as Admin
    participant E as GET /admin/reports/top-products
    participant H as GetTopProductsHandler
    participant Cache as IMemoryCache
    participant DB as PostgreSQL

    A->>E: GET /api/v1/admin/reports/top-products?from=&to=
    E->>H: GetTopProductsQuery

    H->>Cache: TryGetValue(cacheKey)
    alt Cache Hit
        Cache-->>H: Cached result
        H-->>A: 200 OK (from cache)
    else Cache Miss
        H->>DB: SELECT TOP 10 with aggregation query
        DB-->>H: Results
        H->>Cache: Set(cacheKey, results, absoluteExpiration: 3min)
        H-->>A: 200 OK (fresh data)
    end
```

### Cache Invalidation Flow

When an order is created or a product is modified, the handler calls:

```
CacheKeys.InvalidateProducts()   → Resets products version
CacheKeys.InvalidateReports()    → Resets reports version
```

Old cache entries become orphaned (version key changed) and expire by TTL (3–10 minutes).

## 6.4 Scenario: Cancel Order (Stock Restore)

```mermaid
sequenceDiagram
    participant C as Customer
    participant E as POST /orders/{id}/cancel
    participant H as CancelOrderHandler
    participant DB as PostgreSQL
    participant Cache as IMemoryCache

    C->>E: POST /api/v1/orders/{id}/cancel
    E->>H: CancelOrderCommand

    H->>DB: Find order by Id
    H->>H: Verify order belongs to user (or user is Admin)
    H->>H: Verify order status == Pending
    alt Not Pending
        H-->>C: 400 Bad Request<br/>"Only pending orders can be cancelled"
    end
    H->>DB: BEGIN TRANSACTION
    H->>DB: UPDATE Order SET Status = Cancelled
    H->>DB: UPDATE Products<br/>SET Stock = Stock + Quantity<br/>(restore for each item)
    H->>DB: COMMIT
    H->>Cache: InvalidateProductsVersion()<br/>InvalidateReportsVersion()
    H-->>C: 200 OK { cancelled order }
```
