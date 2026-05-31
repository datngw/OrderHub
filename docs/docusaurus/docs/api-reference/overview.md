---
sidebar_position: 1
title: API Overview
description: General conventions, HTTP headers, authentication, versioning, error formats, and rate limiting
---

# API Overview

This reference documentation details the request and response schemas, validation rules, authentication policies, rate limits, and common behaviors for the OrderHub REST API.

---

## 1. Base API URL

All endpoints are exposed over HTTPS. When running locally or inside a Docker environment, the API is exposed on:

```http
http://localhost:5000/api/v1
```

---

## 2. API Versioning Policy

OrderHub uses **URL segment versioning**. The current active version is `v1`. If future API versions are released, they will be accessible under separate paths (e.g., `/api/v2`).

```http
/api/v1/auth/...
/api/v1/products/...
/api/v1/orders/...
/api/v1/admin/reports/...
```

---

## 3. Global HTTP Headers

Clients must configure the following HTTP headers on requests where applicable:

| Header | Type | Required | Description |
|---|:---:|:---:|---|
| **`Authorization`** | string | Yes (auth routes) | The bearer credentials token formatted as: `Bearer <jwt-access-token>`. |
| **`Content-Type`** | string | Yes (write routes) | Set to `application/json` for requests containing body payloads. |
| **`X-Correlation-ID`** | string (GUID) | No | Tracing correlation identifier. If omitted, the API automatically generates one and returns it in the response header. |

---

## 4. Access Levels and Role Policies

| Access Level | Role / Policy Requirements | Example Endpoints |
|---|---|---|
| **Anonymous** | None (public access). | `GET /products`, `/health/ready` |
| **Customer+** | Requires a valid JWT containing either `Customer` or `Admin` roles. | `POST /orders`, `GET /orders/me` |
| **Admin** | Requires a valid JWT containing the `Admin` role. | `POST /products`, `GET /admin/reports/...` |

---

## 5. Standard Paged Results (`PagedResult<T>`)

Endpoints returning lists (e.g., product catalog or customer orders) wrap results in a paginated envelope to control payload sizes and database memory usage:

```json
{
  "items": [],
  "totalCount": 10000,
  "page": 1,
  "pageSize": 20,
  "totalPages": 500
}
```

### Pagination Query Parameters
*   `page` (int, default: `1`): The 1-based page index to retrieve.
*   `pageSize` (int, default: `20`, max: `100`): The number of items to return per page.

---

## 6. Error Responses (RFC 9457 ProblemDetails)

OrderHub standardizes all error payloads to comply with the **RFC 9457 (Problem Details for HTTP APIs)** specification.

The `GlobalExceptionHandler` intercepts exceptions and appends tracing and contextual metrics (`traceId`, `timestamp`, `instance`).

### 6.1 Business Rule / Conflict Failure (409 Conflict)
Returned when a command violates a business invariant (e.g., placing an order with insufficient product stock or creating a product with a duplicate SKU):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Orders.InsufficientStock",
  "status": 409,
  "detail": "Insufficient stock for product 'iPhone 15 Pro Max'. Available: 1, Requested: 2.",
  "instance": "/api/v1/orders",
  "traceId": "0HN123456789A:00000001",
  "timestamp": "2026-06-01T01:30:00.1234567Z"
}
```

### 6.2 Request Validation Failure (400 Bad Request)
Returned when client-side input parameters fail validation checks (e.g., incorrect email format or passwords missing special characters). Validation errors are aggregated in the `errors` dictionary:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "ValidationException",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/auth/register",
  "traceId": "0HN123456789A:00000002",
  "timestamp": "2026-06-01T01:32:15.9876543Z",
  "errors": {
    "Email": ["'Email' must be a valid email address."],
    "Password": ["Password must contain at least one uppercase letter."]
  }
}
```

---

## 7. HTTP Status Code Mapping

| Status Code | Meaning | Context / Trigger Event |
|---|---|---|
| **200 OK** | Success | Request completed successfully; returns the requested resource or result. |
| **201 Created** | Created | Creation transaction succeeded; returns the newly created entity and location header. |
| **204 No Content**| No Content | Action succeeded (e.g., updating order status, deleting a product, logging out); returns no response body. |
| **400 Bad Request**| Bad Request | Input parameters failed validation, or JSON structure was unreadable. |
| **401 Unauthorized**| Unauthorized | Authentication token is missing, expired, or signature is invalid. |
| **403 Forbidden** | Forbidden | User role is unauthorized, or customer attempts to access other users' orders. |
| **404 Not Found** | Not Found | Resource ID does not match any active record in the database. |
| **409 Conflict** | Conflict | Business rule violation (e.g., insufficient stock, SKU duplicate, state transition invalid). |
| **429 Too Many Requests**| Rate Limit | Client exceeded their allowed rate limit sliding window. |
| **500 Server Error**| Server Error | Unexpected system exception. Stack traces are redacted in production environments. |

---

## 8. Rate Limiting Configurations

Rate limits are enforced at the API gateway boundary using sliding window limiters. If exceeded, the API responds with **HTTP 429 Too Many Requests**.

*   **Anonymous Requests:** Keyed by client **IP Address**.
*   **Authenticated Sessions:** Keyed by the **User ID** (`sub` claim) inside the JWT token.

| Endpoint Group | Sliding Limit | Window Size | Partition Key | Configuration Policy |
|---|:---:|:---:|---|---|
| **`/api/v1/auth/login`** | 5 requests | 1 minute | Client IP | `auth-login` |
| **`/api/v1/auth/register`** | 3 requests | 1 minute | Client IP | `auth-register` |
| **`/api/v1/auth/refresh`** | 10 requests | 1 minute | Client IP | `auth-refresh` |
| **`/api/v1/products/...`** | 60 requests | 1 minute | User ID / IP | `products` |
| **`/api/v1/orders/...`** | 30 requests | 1 minute | User ID / IP | `orders` |
| **`/api/v1/admin/reports/...`**| 40 requests | 1 minute | User ID / IP | `admin` |
