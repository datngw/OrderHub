---
slug: /
sidebar_position: 1
title: OrderHub Documentation
---

# OrderHub Documentation

Welcome to the **OrderHub** documentation — your comprehensive guide to understanding, developing, and operating the central order management API for e-commerce.

## What is OrderHub?

OrderHub is a **.NET 8 Web API** built with Clean Architecture that provides:

- **Product Catalog** — CRUD with soft delete, paginated filtering, search, and sorting
- **Order Management** — Atomic creation with pessimistic locking to prevent overselling
- **Authentication** — JWT access tokens + refresh tokens with role-based access
- **Admin Reporting** — Cached revenue and product analytics
- **Observability** — Structured logging via Serilog with Seq visualization

## Documentation Sections

### 🚀 [Getting Started](/docs/getting-started/quick-start)
Get OrderHub running locally in 5 minutes with Docker Compose or a manual setup.

### 🏗️ [Architecture (arc42)](/docs/architecture/introduction-goals)
Deep dive into the system architecture following the **arc42 template** — 12 sections covering goals, constraints, building blocks, runtime scenarios, deployment, and architectural decisions.

### 📡 [API Reference](/docs/api-reference/overview)
Complete endpoint documentation with request/response examples, authentication requirements, and validation rules.

### 📖 [Guides](/docs/guides/deployment)
Operational guides for deployment, observability, and caching strategies.

## Tech Stack at a Glance

| Area | Technology |
|------|-----------|
| Runtime | .NET 8 (LTS) |
| Database | PostgreSQL 16 |
| Auth | JWT + PasswordHasher\<T\> |
| CQRS | MediatR 14.1.0 |
| Validation | FluentValidation 12.1.1 |
| Mapping | Mapster 10.0.7 |
| Logging | Serilog → Console + File + Seq |
| Testing | xUnit + FluentAssertions + Moq + Testcontainers |
| Containers | Docker + docker-compose |
