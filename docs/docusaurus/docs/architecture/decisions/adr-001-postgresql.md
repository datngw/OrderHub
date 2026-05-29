---
sidebar_position: 2
title: "ADR-001: PostgreSQL over SQL Server"
description: Decision to use PostgreSQL 16 as the primary relational database
---

# ADR-001: PostgreSQL over SQL Server

## Status

✅ Accepted

## Context

OrderHub requires a relational database that supports row-level locking for concurrency control. The system must handle concurrent order creation where multiple users attempt to purchase the same product simultaneously. The database needs to integrate well with Entity Framework Core and support Docker-based local development.

## Decision

Use **PostgreSQL 16** as the primary database, accessed via EF Core 8 with the Npgsql provider.

## Rationale

Three options were considered:

| Criteria | PostgreSQL | SQL Server | MySQL |
|----------|-----------|------------|-------|
| **Licensing** | Open-source, free | Requires paid license for production | Open-source (community edition) |
| **Row-level locking** | Mature `SELECT ... FOR UPDATE` | Supported via `UPDLOCK` hints | Limited (gap locking issues) |
| **EF Core provider** | Npgsql (mature, first-class) | Microsoft official | Pomelo (community) |
| **Docker support** | Excellent, lightweight images | Large image, license complexity | Good |
| **JSON support** | Native JSONB | JSON support (2016+) | JSON support |

PostgreSQL was chosen because it provides mature row-level locking essential for pessimistic locking (ADR-002), has zero licensing cost, and the Npgsql provider offers first-class EF Core support. SQL Server was a strong contender but its licensing cost and heavier Docker image made it less suitable for an open-source project.

## Consequences

**Positive:**
- Open-source with no licensing cost
- Mature `SELECT ... FOR UPDATE` support for pessimistic locking
- Npgsql provider provides first-class EF Core integration
- Lightweight Docker images for local development
- Native JSONB support for future semi-structured data needs

**Negative:**
- Requires PostgreSQL-specific expertise (query optimization, monitoring)
- Some EF Core features behave differently than with SQL Server provider
- Tooling ecosystem is smaller than SQL Server (SSMS, Profiler)
