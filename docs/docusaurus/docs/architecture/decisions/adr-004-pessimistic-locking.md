---
sidebar_position: 5
title: "ADR-004: Pessimistic Locking for Stock Control"
description: Decision to use pessimistic row-level locking (SELECT ... FOR UPDATE) to prevent stock overselling under concurrent checkouts
---

# ADR-004: Pessimistic Locking for Stock Control

## Status

✅ Accepted

## Context

OrderHub is an order management API that must support concurrent product checkouts. A critical business requirement is **Inventory Integrity**: the system must never sell more units of a product than are available in stock (preventing double-selling or overselling).

Under high concurrent traffic (e.g., flash sales):
*   Multiple customers can attempt to checkout the same product at the same time.
*   If two requests read the stock levels concurrently and both see sufficient stock, both will proceed to deduct stock and create orders.
*   This leads to race conditions, database inconsistencies, and stock overselling.

The system needs a concurrency control strategy that guarantees inventory correctness under heavy concurrent load.

## Decision

Implement **Pessimistic Row-Level Locking** at the database layer during the order creation transaction. When querying products for checkout validation, the application executes a `SELECT ... FOR UPDATE` statement, locking the product rows until the transaction commits or rolls back.

```csharp
// Repository method executing pessimistic row locking
public async Task<List<Product>> GetByIdsForUpdateAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
{
    return await context.Products
        .FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Id\" = ANY({0}) FOR UPDATE", ids.ToArray())
        .ToListAsync(cancellationToken);
}
```

## Rationale

Two concurrency strategies were evaluated:

| Concurrency Strategy | Pessimistic Locking (`FOR UPDATE`) | Optimistic Locking (`ConcurrencyToken`) |
|---|---|---|
| **Stock Correctness Guarantee** | ✅ 100% (Row lock prevents concurrent writes/reads for update) | ✅ 100% (Version verification fails on update clash) |
| **Transaction Failure Rate** | ✅ Low (Threads block and wait for lock release) | ❌ High (Clashing updates cause exceptions and rollbacks) |
| **Client-Side Complexity** | ✅ Simple (Requests succeed sequentially) | ❌ High (Requires retry loops or error handling on client) |
| **Database Lock Duration** | Longer (Locks held during checkout validation) | Short (Lock check only on commit) |
| **Flash Sale Suitability** | ✅ Recommended (Handles high contention smoothly) | ❌ Poor (High failure rate under high contention) |

Pessimistic locking was chosen because:
1.  **Guarantees Correctness under High Contention:** Flash sales involve high contention on a small number of product items. Pessimistic locking handles this contention at the database layer, serializing requests smoothly.
2.  **Prevents Wasted CPU Cycles:** In contrast, optimistic locking allows concurrent requests to execute validation and calculation logic, only to reject and roll back the transaction on commit, wasting server and database resources.
3.  **Simplifies Client Integration:** Eliminates the need for clients to implement complex checkout retry policies, as requests simply wait for the lock to release.

## Consequences

**Positive:**
*   Guarantees 100% stock correctness under heavy concurrent traffic.
*   Prevents transaction failures and rollbacks under high contention.
*   Simplifies client-side API integration.

**Negative:**
*   **Connection Blocking:** Blocked threads hold database connections longer, increasing connection pool usage.
*   **Deadlock Risk:** If transactions attempt to lock rows in different orders (e.g., transaction A locks product 1 then 2; transaction B locks 2 then 1), deadlocks can occur. (Mitigated by sorting product IDs before executing locks).
*   **Throughput Limits:** Serializing checkouts limits maximum write throughput for a single product to the database's locking speed.
