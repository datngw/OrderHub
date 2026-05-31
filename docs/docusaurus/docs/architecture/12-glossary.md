---
sidebar_position: 12
title: "12. Glossary"
description: Technical and domain terms used throughout OrderHub
---

# 12. Glossary

This glossary defines the domain and technical terms used throughout the OrderHub project.

## 12.1 Domain Terminology

The following terms define the business vocabulary of the e-commerce domain:

| Term | Definition |
|---|---|
| **Order** | A transactional request representing a customer's commitment to purchase catalog products. Follows a structured status lifecycle: `Pending` -> `Confirmed` -> `Shipped` -> `Delivered` (or `Cancelled`). |
| **Order Item** | A line item within an order record, linking a specific product, quantity, and snapshotted unit price. |
| **Product** | An active catalog item available for purchase, identified by a unique SKU. Includes name, description, category, price, and inventory stock level. |
| **SKU** | Stock Keeping Unit — a unique, alphanumeric identifier assigned to each product type in the catalog (e.g., `SKU-ELEC-0001`). |
| **Soft Delete** | A database record deletion strategy. Rather than executing a hard database `DELETE` statement, the application sets a flag (`IsActive = false`), preserving referential integrity for historical orders referencing the product. |
| **Stock** | The physical count of a product available in the warehouse. Decreased atomically during checkout, and restored if an order is cancelled before shipping. |
| **Price Snapshot** | The act of copying a product's current catalog price into an order item's `UnitPrice` record during checkout. This ensures the pricing of completed transactions remains unaffected by subsequent catalog price changes. |
| **Category** | A label used to group products in the catalog (e.g., `Electronics`, `Clothing`). |
| **User Account** | A registered identity within the application, assigned a security role (such as `Customer` or `Admin`). |
| **Refresh Token** | A long-lived, database-backed security token (7 days) used to rotate expired JWT access tokens without forcing the user to re-enter their credentials. |

---

## 12.2 Architectural & Technical Terminology

The following terms define the software engineering concepts, design patterns, and libraries used to build OrderHub:

| Term | Definition |
|---|---|
| **Clean Architecture** | An architectural pattern that organizes code into layers, enforcing that dependencies point strictly inward. The core business domain has no knowledge of databases, delivery protocols (like HTTP), or external frameworks. |
| **CQRS** | Command Query Responsibility Segregation — a design pattern that separates read paths (Queries) from write paths (Commands) at the application layer, using MediatR to handle request routing. |
| **Command** | A MediatR request object representing an operation that mutates state (e.g., creating an order or updating a product). |
| **Query** | A MediatR request object representing a read-only query that retrieves data without causing side effects. |
| **Handler** | An application-layer class responsible for executing the business logic of a specific MediatR Command or Query. |
| **Pipeline Behavior** | MediatR interceptors that wrap handler execution. Used to implement cross-cutting concerns like logging, validation, and performance tracing. |
| **Result Pattern** | A design pattern that uses a return container (`Result<T>`) indicating success or failure instead of throwing exceptions to represent expected business failures (e.g., validation errors). |
| **ProblemDetails** | An RFC 9457 compliant JSON schema that standardizes error responses for HTTP APIs. |
| **Pessimistic Locking** | A concurrency control strategy that locks database rows on read (using `SELECT ... FOR UPDATE`), blocking other transactions from modifying those rows until the lock holder commits or rolls back. |
| **Repository** | A design pattern that encapsulates data access logic behind an interface, decoupling application logic from persistence frameworks like EF Core. |
| **Unit of Work** | A design pattern that maintains a list of entities affected by a business transaction, coordinating writes and transaction boundaries. |
| **DTO** | Data Transfer Object — a data container class used to pass serialized data across application boundaries (e.g., API requests and responses). DTOs are kept separate from database entities. |
| **FluentValidation** | A third-party .NET validation library that uses a fluent API to build strongly-typed validation rules. |
| **Mapster** | A high-performance object mapping library that uses compile-time code generation to map objects between DTOs and database entities. |
| **MediatR** | A library implementing the mediator pattern in .NET, decoupling request dispatching from request handling. |
| **Minimal API** | ASP.NET Core's lightweight model for declaring HTTP endpoints, reducing the performance and code overhead of traditional MVC controller structures. |
| **IMemoryCache** | A local, in-process caching implementation provided by the .NET runtime. |
| **Version-Key Pattern** | A caching strategy that appends a version token to cache keys. On state mutation, the version token is deleted, invalidating all matching cached lists on their next read without iterating through individual keys. |
| **Serilog** | A structured logging framework for .NET that outputs logs in machine-readable JSON format. |
| **Seq** | A developer-facing structured log server and query UI used in development to search, filter, and analyze structured application logs. |
| **Testcontainers** | A library that manages lightweight, throwaway Docker container instances (e.g., PostgreSQL) during integration tests. |
| **WebApplicationFactory** | An ASP.NET Core testing utility that boots the web application in-memory during integration testing. |
| **JWT** | JSON Web Token — a cryptographically signed JSON payload used to transmit user authentication claims between clients and the API. |
| **PBKDF2** | Password-Based Key Derivation Function 2 — a standard key-stretching hashing algorithm used by ASP.NET Core's `PasswordHasher<T>` to secure user passwords. |
| **Correlation ID** | A unique request tracing token passed through HTTP headers and Serilog's `LogContext`, allowing developers to correlate logs across services for a single transaction. |
| **AsSplitQuery** | An EF Core query strategy that loads included child collections using separate SQL queries, avoiding the performance overhead of Cartesian products in single-query joins. |
| **HtmlSanitizer** | A library that parses HTML strings and strips potentially malicious tags and attributes, protecting against stored Cross-Site Scripting (XSS) attacks. |
| **Cache Stampede** | Also known as the *Thundering Herd* problem. Occurs when a high-traffic cache entry expires, causing concurrent client requests to query the database simultaneously, potentially degrading database performance. |
| **pg_trgm** | A PostgreSQL database extension that provides functions and operators for determining text similarity based on 3-character sequences (trigrams). Used to build GIN indexes. |
| **GIN Trigram Index** | A Generalized Inverted Index (GIN) configured with trigram operators (`gin_trgm_ops`). Enables the database to run fast case-insensitive substring search queries using the `ILIKE` operator. |
