---
sidebar_position: 3
title: Products
description: Product catalog CRUD endpoints
---

# Products

## List Products

Browse the product catalog with filtering, search, sorting, and pagination.

```
GET /api/v1/products
```

**Auth:** None (public)

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 10 | Items per page (max 100) |
| `search` | string | — | Search in name and description |
| `category` | string | — | Filter by category |
| `minPrice` | decimal | — | Minimum price filter |
| `maxPrice` | decimal | — | Maximum price filter |
| `sortBy` | string | name | Sort field: name, price, createdAt |
| `sortOrder` | string | asc | Sort direction: asc, desc |

### Response (200 OK)

```json
{
  "items": [
    {
      "id": "guid",
      "sku": "SKU-ELEC-0001",
      "name": "Wireless Headphones",
      "description": "Premium noise-cancelling headphones",
      "price": 149.99,
      "stock": 50,
      "category": "Electronics",
      "isActive": true,
      "createdAt": "2025-01-15T10:30:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 10,
  "totalPages": 10
}
```

---

## Get Product by ID

```
GET /api/v1/products/{id}
```

**Auth:** None (public)

### Response (200 OK)

```json
{
  "id": "guid",
  "sku": "SKU-ELEC-0001",
  "name": "Wireless Headphones",
  "description": "Premium noise-cancelling headphones",
  "price": 149.99,
  "stock": 50,
  "category": "Electronics",
  "isActive": true,
  "createdAt": "2025-01-15T10:30:00Z"
}
```

---

## Create Product

```
POST /api/v1/products
```

**Auth:** Admin

### Request

```json
{
  "sku": "SKU-ELEC-0050",
  "name": "Bluetooth Speaker",
  "description": "Portable waterproof speaker",
  "price": 79.99,
  "stock": 100,
  "category": "Electronics"
}
```

### Validation Rules

| Field | Rules |
|-------|-------|
| SKU | Required, unique, max 50 chars |
| Name | Required, max 200 chars |
| Description | Optional, max 2000 chars |
| Price | Required, > 0, max 18 digits with 2 decimal places |
| Stock | Required, ≥ 0 |
| Category | Required, max 100 chars |

### Response (201 Created)

Returns the created product with generated `id` and `createdAt`.

---

## Update Product

```
PUT /api/v1/products/{id}
```

**Auth:** Admin

### Request

```json
{
  "name": "Updated Product Name",
  "description": "Updated description",
  "price": 89.99,
  "stock": 75,
  "category": "Electronics"
}
```

### Response (200 OK)

Returns the updated product.

---

## Delete Product (Soft Delete)

```
DELETE /api/v1/products/{id}
```

**Auth:** Admin

### Response (204 No Content)

The product is marked as `IsActive = false` but not physically removed from the database.
