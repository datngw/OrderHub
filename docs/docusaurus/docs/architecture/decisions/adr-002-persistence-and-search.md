---
sidebar_position: 3
title: "ADR-002: PostgreSQL Persistence with GIN Trigram Search"
description: Decision to use PostgreSQL 16 as the primary database and configure a GIN Trigram index for search optimization
---

# ADR-002: PostgreSQL Persistence with GIN Trigram Search

## Status

✅ Accepted

## Context

OrderHub requires a reliable, ACID-compliant database to persist business data (User accounts, Product catalog, Orders). The persistence layer must support:
*   ACID transactions to guarantee data consistency during order checkouts.
*   Row-level locking (`SELECT ... FOR UPDATE`) to handle concurrent inventory checks.
*   Case-insensitive substring queries (e.g., matching `iPhone` in `iPhone 15 Pro Max`) for product searches in a catalog containing over 10,000 items.
*   Low query latency (under 50ms) for searches, without introducing the operational complexity of external search clusters (like Elasticsearch) during the MVP phase.

## Decision

Use **PostgreSQL 16** as the primary relational database, and optimize product search using a **GIN (Generalized Inverted Index) Trigram Index** on the `Product.Name` column by enabling the **`pg_trgm`** database extension.

### EF Core Configurations
```csharp
// DbContext configuration (OrderHubDbContext.cs)
modelBuilder.HasPostgresExtension("pg_trgm");

// Index configuration (ProductConfiguration.cs)
builder.HasIndex(p => p.Name)
    .HasMethod("gin")
    .HasOperators("gin_trgm_ops")
    .HasDatabaseName("IX_Products_Name_Trgm");

// Query execution (ProductRepository.cs)
query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
```

## Rationale

The database engine and indexing choices were evaluated against typical alternatives:

| Attribute | PostgreSQL 16 + pg_trgm | SQL Server (MSSQL) | PostgreSQL + Elasticsearch |
|---|---|---|---|
| **ACID Compliance** | ✅ High | ✅ High | ⚠️ Eventual consistency (ES sync) |
| **Row-level locking** | ✅ Highly mature | ✅ Highly mature | ❌ N/A |
| **Substring Search Indexing** | ✅ Optimized via Trigrams | ❌ Scans table | ✅ High performance |
| **Operational Complexity** | Low (Single DB container) | Medium | High (requires syncing and cluster maintenance) |
| **License Cost** | ✅ Open source ($0) | Commercial | Open source / Commercial |

PostgreSQL was chosen because:
1.  **Mature Relational Features:** Offers industry-standard transactional reliability, concurrency controls, and row locking (`FOR UPDATE`).
2.  **No Extra Infrastructure for Search:** Enabling the `pg_trgm` extension allows the database to index case-insensitive substring searches natively. This avoids the need to deploy and manage a separate Elasticsearch cluster and sync pipeline.
3.  **Performance at MVP Scale:** The GIN Trigram index reduces query times for product name searches from several seconds (sequential table scans) to under 50ms for datasets under 100,000 records.

## Consequences

**Positive:**
*   Ensures strong transactional guarantees (ACID) and robust locking mechanisms.
*   Keeps infrastructure simple by hosting both transactional data and search indexes in a single database instance.
*   Significantly improves query performance for product searches.

**Negative:**
*   **Write Latency:** GIN indexes are slower to update than standard B-tree indexes, slightly increasing the execution time of product creation and update operations.
*   **Storage Overhead:** Trigram indexes consume more disk space than standard indexes due to the indexing of 3-character slices.
*   **Process-Bound Search Limits:** While suitable for MVP scale, trigram indexing does not support advanced search features (like fuzzy matching, synonyms, or relevance scoring) provided by dedicated search engines.
