---
sidebar_position: 4
title: "ADR-003: Specific Repository and Unit of Work Patterns"
description: Decision to use specific repository interfaces per entity and a central Unit of Work for transactional coordination
---

# ADR-003: Specific Repository and Unit of Work Patterns

## Status

✅ Accepted

## Context

The application needs a strategy to handle database access and transaction management. The data access layer must:
*   Deconstruct queries into clean, testable interfaces, keeping the Application layer independent of EF Core namespaces.
*   Prevent EF Core-specific database code (like `SaveChangesAsync`) from being called directly inside business handlers.
*   Coordinate changes across multiple repositories (e.g., deducting product stock and creating an order) within a single database transaction.
*   Support easy mocking of database queries to facilitate unit testing.

## Decision

Implement **Specific Repositories** per core entity (e.g., `IProductRepository`, `IOrderRepository`) to encapsulate querying logic, and use a central **`IUnitOfWork`** interface to manage transaction boundaries.

```
       +---------------------------------------------+
       |             Application Handlers            |
       +-------+-----------------------------+-------+
               |                             |
               v Uses                        v Uses
       +-------+-------+             +-------+-------+
       | Repositories  |             |  IUnitOfWork  |
       |  (Interfaces) |             |  (Interface)  |
       +-------+-------+             +-------+-------+
               |                             |
               +--------------+--------------+
                              | Implemented by
                              v
               +--------------+--------------+
               |    EF Core Database Context  |
               +-----------------------------+
```

### Usage in Command Handlers
```csharp
public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
{
    // 1. Execute transactional operations through specific repositories
    var products = await _productRepository.GetByIdsForUpdateAsync(request.Items.Select(i => i.ProductId));
    var order = Order.Create(request.UserId, items);
    
    _orderRepository.Add(order);

    // 2. Commit operations atomically via UnitOfWork
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    return Result.Success(order.Id);
}
```

## Rationale

Three data access patterns were evaluated:

| Attribute | Specific Repositories + UoW | Generic Repository | Direct DbContext Injection |
|---|---|---|---|
| **Application Layer Coupling** | ✅ None (Interfaces map to Domain) | ⚠️ Moderate | ❌ High (depends on EF Core) |
| **Query Optimization** | ✅ Highly Optimized (custom LINQ queries) | ❌ Poor (generic methods restrict LINQ tuning) | ✅ Highly Optimized |
| **Transaction Coordination** | ✅ Centralized (via `IUnitOfWork`) | ⚠️ Distributed | ❌ Implicit |
| **Mocking & Unit Testing** | Easy (simple mock interfaces) | Easy | ❌ Difficult (requires complex DbContext setups) |

Specific Repositories and Unit of Work patterns were chosen because:
1.  **Strict Layer Isolation:** Keeps EF Core and database-specific code isolated inside the Infrastructure layer. Query handlers interact only with abstraction interfaces defined in the Domain.
2.  **Optimized Custom Queries:** Allows developers to write optimized LINQ queries (using covering indexes, split queries, and eager loading) encapsulated within repository implementations, rather than polluting handlers with complex database queries.
3.  **Atomic Transactions:** The `IUnitOfWork` interface provides explicit methods to manage transaction boundaries (`BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`), ensuring that operations across multiple repositories either succeed or fail as a single atomic unit.

## Consequences

**Positive:**
*   Ensures a clean separation of concerns, keeping the Application layer independent of EF Core.
*   Simplifies unit testing by allowing repositories to be mocked.
*   Centralizes query optimization inside repository implementation classes.
*   Enforces explicit transaction boundaries.

**Negative:**
*   **Boilerplate:** Requires creating both an interface and an implementation class for each new entity repository.
*   **Abstraction Layer Overhead:** Adds a layer of indirection between query execution and the database context.
