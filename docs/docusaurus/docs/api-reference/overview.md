---
sidebar_position: 1
title: API Overview
description: General conventions, authentication, versioning, and pagination
---

# API Overview

## Base URL

```
http://localhost:5000/api/v1
```

## Authentication

Most endpoints require a JWT Bearer token in the `Authorization` header:

```
Authorization: Bearer {access_token}
```

| Auth Level | Description |
|-----------|-------------|
| **No Auth** | Public endpoints — product browsing, health checks |
| **Customer+** | Any authenticated user (Customer or Admin role) |
| **Admin** | Admin role required |

## API Versioning

URL segment versioning — currently only **v1** is available:

```
/api/v1/auth/...
/api/v1/products/...
/api/v1/orders/...
/api/v1/admin/reports/...
```

## Pagination

All list endpoints return a `PagedResult<T>` response:

```json
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 10,
  "totalPages": 10
}
```

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number (1-based) |
| `pageSize` | int | 10 | Items per page (max 100) |

## Error Responses

All errors follow RFC 9457 ProblemDetails format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "FieldName": ["Error message"]
  }
}
```

### Common HTTP Status Codes

| Status | Meaning |
|--------|---------|
| 200 | Success |
| 201 | Created successfully |
| 204 | Deleted successfully (no content) |
| 400 | Validation error or bad request |
| 401 | Missing or invalid JWT token |
| 403 | Insufficient permissions (wrong role or not owner) |
| 404 | Resource not found |
| 409 | Conflict (e.g., insufficient stock) |
| 429 | Rate limit exceeded |

## Rate Limiting

Each endpoint group has its own rate limit:

| Group | Limit | Partition |
|-------|-------|-----------|
| Auth (login) | 5 req/min | IP address |
| Auth (register) | 3 req/min | IP address |
| Products | 60 req/min | User ID / IP |
| Orders | 30 req/min | User ID / IP |
| Admin | 40 req/min | User ID / IP |
