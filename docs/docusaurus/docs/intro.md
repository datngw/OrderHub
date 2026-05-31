---
slug: /
sidebar_position: 1
title: OrderHub Documentation
---

# OrderHub Documentation

Welcome to the **OrderHub** documentation — the comprehensive guide to understanding, developing, and operating the central order management engine for enterprise e-commerce platforms.

OrderHub is built from the ground up on **.NET 8 (LTS)** and **PostgreSQL 16**, following the principles of **Clean Architecture**. It is designed to handle high-concurrency transaction environments, protecting inventory integrity while maintaining sub-50ms response times for catalog queries.

---

## Key Core Features

*   **Pessimistic Concurrency Control:** Prevents stock overselling (double-selling) under heavy checkout traffic (e.g., flash sales) using SQL `SELECT ... FOR UPDATE` row locks.
*   **Clean Architecture & CQRS:** Strict project boundaries (`Api` -> `Infrastructure` -> `Application` -> `Domain`) enforced at compile time. Read and write paths are separated using MediatR.
*   **Search Optimization:** Integrates the PostgreSQL `pg_trgm` extension and GIN Trigram indexing on product names, enabling case-insensitive substring searches across catalogs of over 10,000 active products.
*   **Cache Stampede Protection:** Uses a thread-safe `CacheStampedeGuard` (with key-based `SemaphoreSlim` double-checked locks) to prevent database overload (thundering herd problem) when cache entries expire.
*   **Observability Pipeline:** Pre-configured structured JSON rolling log streams via Serilog, with built-in PII and authorization token masking filters, correlation ID tracking, and Jaeger tracing integration readiness.
*   **Edge Security Hardening:** Core rate-limiting middleware (sliding windows), HTML input sanitization endpoint filters (preventing stored XSS), and NetEscapades security headers.

---

## Documentation Sections

### 🚀 [Getting Started](/docs/getting-started/quick-start)
Get the API and backing services running locally in under 5 minutes using Docker Compose, or set up a manual local development environment.
*   [Quick Start](/docs/getting-started/quick-start) — Start the stack (API, PostgreSQL, pgAdmin, Seq) using a single command.
*   [Local Development](/docs/getting-started/local-development) — Build, run migrations, and debug the API locally.
*   [Running Tests](/docs/getting-started/running-tests) — Execute the test suite (52 unit tests and Testcontainers integration tests).
*   [Seed Data](/docs/getting-started/seed-data) — Understand default test accounts and the **10,000 product seeder**.

### 🏗️ [Architecture (arc42)](/docs/architecture/introduction-goals)
A deep-dive into the architectural design of OrderHub structured after the official **arc42 template**:
*   [Goals & Priorities](/docs/architecture/introduction-goals) — Understand quality goals and stakeholders.
*   [System Boundary](/docs/architecture/context) — Inspect external actors, database interfaces, and protocols.
*   [Solution Strategy](/docs/architecture/solution-strategy) — Read the design decisions behind Clean Architecture, CQRS, and locking.
*   [Building Blocks](/docs/architecture/building-blocks) — Map namespaces and project structures.
*   [Runtime View](/docs/architecture/runtime-view) — View sequence diagrams for checkout locking and cache stampede guards.
*   [Architecture Decisions (ADRs)](/docs/architecture/decisions) — Browse the index of the **6 Core Architectural Decisions**.

### 📡 [API Reference](/docs/api-reference/overview)
Complete endpoint documentation with request/response schemas, validation constraints, and role authorization policies:
*   [Authentication](/docs/api-reference/authentication) — User registration, login, and refresh token rotation.
*   [Products](/docs/api-reference/products) — Paginated lists, filters, searches, and admin catalog controls.
*   [Orders](/docs/api-reference/orders) — Concurrency-safe checkouts, order detail lookups, status management, and cancellation.
*   [Admin Reports](/docs/api-reference/admin-reports) — Revenue metrics and top-selling product reports.

### 📖 [Guides](/docs/guides/caching)
Detailed operational guides to manage and scale the OrderHub stack:
*   [Caching Strategy](/docs/guides/caching) — Version-key in-memory invalidation and stampede guard locks.
*   [Deployment Guide](/docs/guides/deployment) — Production topology scaling and environment variables.
*   [Observability](/docs/guides/observability) — Structured logging sinks, correlation IDs, and PII filters.

---

## Technology Stack

| Component | Technology | Description |
|---|---|---|
| **Runtime** | .NET 8.0 (LTS) | High-performance C# backend runtime environment. |
| **Database** | PostgreSQL 16 | ACID-compliant transactional relational database. |
| **ORM** | EF Core 8 + Npgsql | Entity Framework mapping database tables to C# models. |
| **Validation** | FluentValidation | Compiles use-case constraints and input parameters. |
| **Mapping** | Mapster | High-performance compile-time object mapping. |
| **Logging** | Serilog | Structured JSON log generation. |
| **Testing** | xUnit + Testcontainers | Unit testing (mocked) and integration testing (real PostgreSQL). |
