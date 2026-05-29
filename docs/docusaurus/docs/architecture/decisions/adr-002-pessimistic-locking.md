---
sidebar_position: 3
title: "ADR-002: Pessimistic Locking for Stock Control"
description: Decision to use SELECT FOR UPDATE to prevent overselling under concurrency
---

# ADR-002: Pessimistic Locking for Stock Control

## Status

✅ Accepted

## Context

Under concurrent order creation, multiple requests may read the same product stock value simultaneously and all succeed, leading to overselling. For an e-commerce platform, selling more inventory than available is a critical business failure. Correctness of stock deduction must be guaranteed even under high concurrency.

## Decision

Use **`SELECT ... FOR UPDATE`** (pessimistic locking) to lock product rows during the order creation transaction. This ensures that only one transaction can read and modify a given product's stock at a time.

## Rationale

Three concurrency strategies were evaluated:

| Strategy | Correctness | Throughput | Complexity |
|----------|------------|------------|------------|
| **Pessimistic locking** | Guaranteed (database-enforced) | Reduced under contention | Low (single SQL construct) |
| **Optimistic concurrency** (EF Core `ConcurrencyToken`) | Requires retry logic | Higher under low contention | Medium (retry + conflict resolution) |
| **Serializable isolation** | Guaranteed | Lowest ( widest lock scope) | Medium (database-level setting) |

Pessimistic locking was chosen because:

1. **Correctness is non-negotiable** for commerce — overselling is a worse outcome than reduced throughput
2. **Simplicity** — a single `ForUpdate()` call in EF Core, no retry loops or conflict resolution logic
3. **Verified behavior** — tested with 20 concurrent requests against stock=5, exactly 5 orders succeeded

Optimistic concurrency was rejected because it requires retry logic that is harder to reason about and test for correctness. Serializable isolation was rejected because it locks a wider scope than necessary, further reducing throughput.

## Consequences

**Positive:**
- Correctness guaranteed — verified with 20 concurrent requests / stock=5 → exactly 5 succeed
- Simple implementation at the database level (single `ForUpdate()` call)
- No retry logic or conflict resolution needed
- Predictable behavior under load

**Negative:**
- Reduced throughput under high contention on popular products
- Locks held for the duration of the transaction (until commit/rollback)
- Potential for deadlocks if lock ordering is inconsistent (mitigated by always locking products in a consistent order)
