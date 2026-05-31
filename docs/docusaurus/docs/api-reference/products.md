---
sidebar_position: 3
title: Products
description: Product catalog CRUD endpoints for customers and administrators
---

# Products API

The products API allows customers to browse the product catalog and enables administrators to manage catalog items.

---

## 1. List Products

Retrieves a paginated list of active products from the catalog. Results can be filtered, searched, and sorted.

```http
GET /api/v1/products
```

*   **Authentication:** None (Public)
*   **Rate Limit:** 60 requests per minute per IP or User ID (`products` policy).
*   **Caching:** Product list query results are cached locally in `IMemoryCache` (30s sliding / 5m absolute TTL) and protected by the `CacheStampedeGuard` to prevent database locks.
*   **Search Engine:** Text search utilizes a PostgreSQL **GIN Trigram Index** (`pg_trgm` extension) for fast case-insensitive substring searching.

### Query Parameters

| Parameter | Type | Default | Description |
|---|:---:|:---:|---|
| **`page`** | int | `1` | The 1-based page index to retrieve. |
| **`pageSize`** | int | `20` | The number of products to return per page (maximum: 100). |
| **`search`** | string | — | Substring search pattern matched case-insensitively against product names (uses GIN Index). |
| **`category`** | string | — | Filter products by exact category label (case-sensitive). |
| **`minPrice`** | decimal | — | Filter products with price greater than or equal to this value. |
| **`maxPrice`** | decimal | — | Filter products with price less than or equal to this value. |
| **`sortBy`** | string | `"CreatedAt"` | Sort field. Allowed values (case-insensitive): `createdAt`, `name`, `price`, `category`, `sku`. |
| **`sortOrder`** | string | `"desc"` | Sort direction. Allowed values (case-insensitive): `asc`, `desc`. |

### Response (200 OK - PagedResult)
```json
{
  "items": [
    {
      "id": "e2a3b4c5-1234-5678-abcd-1e2f3a4b5c6d",
      "sku": "SKU-00001",
      "name": "iPhone 15 Pro Max",
      "description": "High-quality electronics product. Reliable performance.",
      "price": 1199.99,
      "stock": 150,
      "category": "Electronics",
      "isActive": true,
      "createdAt": "2026-06-01T01:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

## 2. Get Product by ID

Retrieves detailed information for a specific active product catalog item.

```http
GET /api/v1/products/{id}
```

*   **Authentication:** None (Public)
*   **Caching:** Individual product lookup results are cached (30s sliding / 10m absolute TTL) and protected by `CacheStampedeGuard`.

### Path Parameters
*   `id` (guid, required): The unique identifier of the target product.

### Response (200 OK)
```json
{
  "id": "e2a3b4c5-1234-5678-abcd-1e2f3a4b5c6d",
  "sku": "SKU-00001",
  "name": "iPhone 15 Pro Max",
  "description": "High-quality electronics product. Reliable performance.",
  "price": 1199.99,
  "stock": 150,
  "category": "Electronics",
  "isActive": true,
  "createdAt": "2026-06-01T01:00:00Z"
}
```

### Error Responses
*   **404 Not Found**: Returned if the ID does not match any product, or if the matched product has been soft-deleted.

---

## 3. Create Product

Adds a new product catalog item (Admin only).

```http
POST /api/v1/products
```

*   **Authentication:** Required (`Admin` role)
*   **Cache Invalidation:** Instantly increments the product catalog cache version token, orphaning all cached product lists.

### Request Body (JSON)
```json
{
  "sku": "SKU-00101",
  "name": "Sony WH-1000XM5",
  "description": "Premium noise-cancelling wireless headphones",
  "price": 349.99,
  "stock": 50,
  "category": "Electronics"
}
```

### Input Validation Rules

| Property | Required | Type | Rules & Constraints |
|---|:---:|:---:|---|
| **`sku`** | Yes | string | Unique SKU code. Max 50 characters. |
| **`name`** | Yes | string | Max 200 characters. Cleansed of XSS script inputs at the API boundary. |
| **`description`**| No | string | Max 2000 characters. |
| **`price`** | Yes | decimal | Must be strictly greater than 0. Precision up to 18 digits total, 2 decimal places. |
| **`stock`** | Yes | int | Inventory count. Must be greater than or equal to 0. |
| **`category`** | Yes | string | Max 100 characters. |

### Response (201 Created)
Returns the newly created product. The HTTP `Location` response header points to the detail route of the created resource.
```json
{
  "id": "a9b8c7d6-1234-5678-abcd-1e2f3a4b5c6d",
  "sku": "SKU-00101",
  "name": "Sony WH-1000XM5",
  "description": "Premium noise-cancelling wireless headphones",
  "price": 349.99,
  "stock": 50,
  "category": "Electronics",
  "isActive": true,
  "createdAt": "2026-06-01T01:15:30Z"
}
```

### Error Responses
*   **409 Conflict**: Returned with code `Products.DuplicateSKU` if the provided SKU is already registered in the system.

---

## 4. Update Product

Updates the editable properties of an existing product (Admin only).

```http
PUT /api/v1/products/{id}
```

*   **Authentication:** Required (`Admin` role)
*   **Cache Invalidation:** Evicts specific item cache (`products:byid:{id}`) and invalidates list caches.

### Path Parameters
*   `id` (guid, required): The unique identifier of the product to update.

### Request Body (JSON)
```json
{
  "name": "Sony WH-1000XM5 (Updated Edition)",
  "description": "Premium noise-cancelling wireless headphones - Refined description",
  "price": 329.99,
  "stock": 45,
  "category": "Electronics"
}
```

:::note SKU Immutability
The product **SKU is immutable** and cannot be modified after creation. It is omitted from the update request payload.
:::

### Response (200 OK)
Returns the fully updated product object:
```json
{
  "id": "a9b8c7d6-1234-5678-abcd-1e2f3a4b5c6d",
  "sku": "SKU-00101",
  "name": "Sony WH-1000XM5 (Updated Edition)",
  "description": "Premium noise-cancelling wireless headphones - Refined description",
  "price": 329.99,
  "stock": 45,
  "category": "Electronics",
  "isActive": true,
  "createdAt": "2026-06-01T01:15:30Z"
}
```

---

## 5. Delete Product

Applies a soft-delete to a product catalog item (Admin only).

```http
DELETE /api/v1/products/{id}
```

*   **Authentication:** Required (`Admin` role)
*   **Cache Invalidation:** Evicts the item details cache and invalidates query list caches.

### Path Parameters
*   `id` (guid, required): The unique identifier of the product to soft-delete.

### Response (204 No Content)
Returns an empty response indicating success.

:::tip Data Integrity
Soft-deleted products are flagged as `IsActive = false` in the database. They will no longer appear in public catalog queries or be purchasable in new checkouts. However, the database row is preserved to maintain referential integrity for existing historical order line items.
:::
