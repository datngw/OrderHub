---
sidebar_position: 5
title: Admin Reports
description: Analytical reporting endpoints for revenue and sales performance
---

# Admin Reports API

The admin reports API exposes analytical endpoints enabling administrators to track business performance.

---

## 1. Top Products by Revenue

Retrieves a ranked list of products generating the highest sales revenue within the specified date range.

```http
GET /api/v1/admin/reports/top-products
```

*   **Authentication:** Required (`Admin` role)
*   **Rate Limit:** 40 requests per minute per IP or User ID (`admin` policy).
*   **Caching:** Results are cached in `IMemoryCache` for **3 minutes** (Absolute expiration) using the `CacheStampedeGuard` to prevent database lockups under concurrent reads.
*   **Cache Invalidation:** The underlying report cache version token is incremented on any product modification (create, update, delete) or order state change (create, status transition, cancel).

### Query Parameters

| Parameter | Type | Required | Default | Description |
|---|:---:|:---:|---|---|
| **`from`** | datetime | No | 30 days ago | UTC start timestamp (ISO 8601 format: `YYYY-MM-DDTHH:MM:SSZ`). |
| **`to`** | datetime | No | Current time | UTC end timestamp (ISO 8601 format: `YYYY-MM-DDTHH:MM:SSZ`). |
| **`top`** | int | No | `10` | The maximum number of top products to return. |

### Response (200 OK - JSON Array)
Returns a list of `TopProductRevenueResponse` elements:
```json
[
  {
    "productId": "e2a3b4c5-1234-5678-abcd-1e2f3a4b5c6d",
    "productName": "iPhone 15 Pro Max",
    "totalQuantity": 15,
    "totalRevenue": 17999.85
  },
  {
    "productId": "f9a8b7c6-1234-5678-abcd-1e2f3a4b5c6d",
    "productName": "MacBook Air M3",
    "totalQuantity": 5,
    "totalRevenue": 4999.95
  }
]
```

---

## 2. Revenue by Day

Retrieves total sales revenue and order volumes aggregated by day within the specified date range.

```http
GET /api/v1/admin/reports/revenue-by-day
```

*   **Authentication:** Required (`Admin` role)
*   **Rate Limit:** 40 requests per minute per IP or User ID (`admin` policy).
*   **Caching:** Results are cached in `IMemoryCache` for **3 minutes** (Absolute expiration) protected by the `CacheStampedeGuard`.
*   **Cache Invalidation:** Same rules as the top products endpoint (invalidated on product mutations or order transactions).

### Query Parameters

| Parameter | Type | Required | Default | Description |
|---|:---:|:---:|---|---|
| **`from`** | datetime | No | 30 days ago | UTC start timestamp (ISO 8601 format). |
| **`to`** | datetime | No | Current time | UTC end timestamp (ISO 8601 format). |

### Response (200 OK - JSON Array)
Returns a list of `RevenueByDayResponse` elements, representing daily aggregates:
```json
[
  {
    "date": "2026-06-01T00:00:00",
    "orderCount": 3,
    "totalRevenue": 3599.97
  },
  {
    "date": "2026-05-31T00:00:00",
    "orderCount": 1,
    "totalRevenue": 2399.98
  }
]
```
