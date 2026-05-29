---
sidebar_position: 5
title: Admin Reports
description: Cached admin reporting endpoints for revenue and product analytics
---

# Admin Reports

## Top Products by Revenue

Get the top 10 products ranked by total revenue within a date range.

```
GET /api/v1/admin/reports/top-products
```

**Auth:** Admin

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `from` | datetime | No | Start date filter (default: 30 days ago) |
| `to` | datetime | No | End date filter (default: now) |

### Caching

- **TTL:** Absolute 3 minutes
- **Invalidation:** Cache version reset on any order or product mutation

### Response (200 OK)

```json
[
  {
    "productId": "guid",
    "productName": "Wireless Headphones",
    "sku": "SKU-ELEC-0001",
    "totalQuantitySold": 150,
    "totalRevenue": 22498.50
  },
  {
    "productId": "guid",
    "productName": "Bluetooth Speaker",
    "sku": "SKU-ELEC-0002",
    "totalQuantitySold": 89,
    "totalRevenue": 7119.11
  }
]
```

---

## Revenue by Day

Get revenue aggregated by day within a date range.

```
GET /api/v1/admin/reports/revenue-by-day
```

**Auth:** Admin

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `from` | datetime | No | Start date filter (default: 30 days ago) |
| `to` | datetime | No | End date filter (default: now) |

### Caching

- **TTL:** Absolute 3 minutes
- **Invalidation:** Cache version reset on any order or product mutation

### Response (200 OK)

```json
[
  {
    "date": "2025-06-14",
    "totalRevenue": 1549.97,
    "orderCount": 12
  },
  {
    "date": "2025-06-13",
    "totalRevenue": 899.95,
    "orderCount": 7
  }
]
```
