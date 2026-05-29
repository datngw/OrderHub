---
sidebar_position: 7
title: "ADR-006: Specific Repository + Unit of Work"
description: Decision to use entity-specific repository interfaces with a Unit of Work for transaction management
---

# ADR-006: Specific Repository + Unit of Work

## Status

✅ Accepted

## Context

The Application layer needs a persistence abstraction over EF Core `DbContext` that follows the dependency inversion principle. Repository interfaces are defined in the Domain layer (which has zero external dependencies) and implemented in the Infrastructure layer. The abstraction must support transaction management for operations that span multiple entities (e.g., order creation modifies both products and orders).

## Decision

Each entity gets its own **specific repository interface** (`IProductRepository`, `IOrderRepository`, `IUserRepository`, `IRefreshTokenRepository`) defined in Domain and implemented in Infrastructure. An **`IUnitOfWork`** wraps `SaveChanges` and transaction management (`BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`).

## Rationale

Three persistence patterns were considered:

| Criteria | Specific Repository + UoW | Generic Repository (`IRepository<T>`) | Direct DbContext |
|----------|--------------------------|---------------------------------------|------------------|
| **Focused contracts** | ✅ Purpose-built per entity | ❌ One-size-fits-all API | ❌ Exposes full DbSet |
| **Testability** | ✅ Easy to mock specific methods | ⚠️ Overly broad mock surface | ⚠️ Complex to mock DbContext |
| **Domain purity** | ✅ Interfaces in Domain, zero deps | ⚠️ Generic often leaks EF concerns | ❌ Ties Application to EF Core |
| **Transaction boundaries** | ✅ Explicit via UoW | ⚠️ UoW still needed | ⚠️ Implicit in SaveChanges |
| **Boilerplate** | More interfaces per entity | Single interface for all | Least code |

Specific repositories were chosen because:

1. **Focused contracts** — each interface exposes only the operations relevant to that entity (e.g., `IProductRepository` has filtering and pagination, `IRefreshTokenRepository` has token validation)
2. **Easy to test and mock** — narrow interfaces mean focused test setups with only the methods needed for each test
3. **Domain purity** — interfaces defined in Domain have zero knowledge of EF Core; Infrastructure implements them
4. **Explicit transaction boundaries** — `IUnitOfWork` makes it clear when a transaction starts and commits

Generic repositories were rejected because they tend to expose a one-size-fits-all API that doesn't match domain needs. Direct DbContext usage was rejected because it couples the Application layer to EF Core, violating the dependency inversion principle.

## Consequences

**Positive:**
- Focused contracts per entity — easier to test and mock
- Explicit transaction boundaries via `IUnitOfWork`
- Domain layer remains free of EF Core dependencies
- Each repository is purpose-built for its entity's access patterns

**Negative:**
- More interfaces to maintain (one per entity)
- More implementation classes in Infrastructure
- Slightly more boilerplate than generic repository or direct DbContext usage
