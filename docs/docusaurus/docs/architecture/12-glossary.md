---
sidebar_position: 12
title: "12. Glossary"
description: Technical and domain terms used throughout OrderHub
---

# 12. Glossary

## Domain Terms

| Term | Definition |
|------|-----------|
| **Order** | A customer's request to purchase one or more products. Has a lifecycle: Pending → Confirmed → Shipped → Delivered (or Cancelled). |
| **OrderItem** | A line item within an order, linking a product with quantity and a snapshotted unit price. |
| **Product** | An item in the catalog identified by a unique SKU. Has name, description, price, stock quantity, category, and active status. |
| **SKU** | Stock Keeping Unit — a unique alphanumeric identifier for a product (e.g., `SKU-ELEC-0001`). |
| **Soft Delete** | Marking a product as `IsActive = false` instead of physically removing it from the database. Preserves referential integrity. |
| **Stock** | The available quantity of a product. Decremented atomically on order creation, restored on cancellation. |
| **Price Snapshot** | The `UnitPrice` captured from `Product.Price` at the moment of order creation. Decouples historical order data from current prices. |
| **Category** | A classification label for products (e.g., Electronics, Clothing). Currently a string field. |
| **User** | An authenticated identity with a role (Admin or Customer) and a registered email. |
| **Refresh Token** | A long-lived token (7 days) stored in the database, used to obtain new JWT access tokens without re-authentication. |

## Architecture & Technical Terms

| Term | Definition |
|------|-----------|
| **Clean Architecture** | A layered architecture pattern where dependencies point inward — Domain has zero external dependencies, and Infrastructure implements Application interfaces. |
| **CQRS** | Command Query Responsibility Segregation — separating write operations (Commands) from read operations (Queries) using MediatR. |
| **Command** | A MediatR request object representing a write operation (create, update, delete). |
| **Query** | A MediatR request object representing a read operation that returns data without side effects. |
| **Handler** | A class that processes a specific Command or Query, containing the business logic. |
| **Pipeline Behavior** | MediatR middleware that wraps handler execution — used for validation, logging, and performance tracking. |
| **Result Pattern** | An explicit success/failure return type (`Result<T>`) instead of throwing exceptions for expected business errors. |
| **ProblemDetails** | RFC 9457 standard format for HTTP API error responses with `type`, `title`, `status`, and `errors` fields. |
| **Pessimistic Locking** | Database-level concurrency control using `SELECT ... FOR UPDATE` to lock rows during a transaction, preventing other transactions from modifying them. |
| **Repository** | A pattern that encapsulates data access logic behind an interface (e.g., `IProductRepository`). |
| **Unit of Work** | A pattern that wraps `SaveChanges` and transaction management, ensuring atomic operations across repositories. |
| **DTO** | Data Transfer Object — a plain object for carrying data between layers. Request DTOs and response DTOs are always separate from domain entities. |
| **FluentValidation** | A .NET library for building strongly-typed validation rules with a fluent interface. |
| **Mapster** | A high-performance object mapping library using compile-time code generation. |
| **MediatR** | A .NET library implementing the Mediator pattern for decoupling request/response logic. |
| **Minimal API** | ASP.NET Core's lightweight approach to defining HTTP endpoints using lambda expressions and extension methods. |
| **IMemoryCache** | .NET's in-memory caching abstraction. Used with a version-key pattern for handler-level caching. |
| **Version-Key Pattern** | Caching strategy where keys include a version number. On mutation, the version is reset, orphaning old cache entries that expire by TTL. |
| **Serilog** | A structured logging library for .NET that supports rich data in log events. |
| **Seq** | A structured log server with a web UI for searching, filtering, and analyzing log events (used in development). |
| **Testcontainers** | A library that provides lightweight, throwaway database instances (Docker containers) for integration tests. |
| **WebApplicationFactory** | An ASP.NET Core testing utility that hosts the API in-memory for integration testing. |
| **JWT** | JSON Web Token — a compact, URL-safe token format for transmitting claims between parties. |
| **PasswordHasher\<T\>** | ASP.NET Core's built-in password hashing utility using PBKDF2 with HMAC-SHA256. |
| **Health Check** | An endpoint that reports the application's operational status. Liveness (process alive) vs. Readiness (ready to serve, including DB connectivity). |
