---
sidebar_position: 4
title: Orders
description: Order checkout, history tracking, status management, and cancellation APIs
---

# Orders API

The orders API handles checkout processing, order history lookups, administrative order lifecycle transitions, and order cancellations.

---

## 1. Create Order (Checkout)

Submits a new order for checkout. The system validates inventory levels and deducts stock atomically in a single database transaction.

```http
POST /api/v1/orders
```

*   **Authentication:** Required (`Customer` or `Admin` role)
*   **Rate Limit:** 30 requests per minute per IP or User ID (`orders` policy).
*   **Concurrency Control:** Employs database-level **Pessimistic Locking** (`SELECT ... FOR UPDATE` locks on the product rows). Under concurrent purchase attempts, transactions queue up and execute sequentially, completely preventing inventory overselling.
*   **Cache Invalidation:** Instantly increments the report cache version key, orphaning all admin analytics caches.

### Request Body (JSON)
```json
{
  "items": [
    {
      "productId": "e2a3b4c5-1234-5678-abcd-1e2f3a4b5c6d",
      "quantity": 2
    }
  ]
}
```

### Input Validation Rules

| Property | Required | Type | Rules & Constraints |
|---|:---:|:---:|---|
| **`items`** | Yes | array | Must contain at least 1 item line. |
| **`productId`** | Yes | guid | Must match an existing, active product catalog item. |
| **`quantity`** | Yes | int | Must be strictly greater than 0. Must not exceed available stock. |

### Response (201 Created)
Returns the created order details payload. The `Location` response header points to the order detail endpoint.
```json
{
  "id": "b1b2b3b4-1234-5678-abcd-9e2f3a4b5c6d",
  "userId": "d5e6f7a8-1234-5678-abcd-1e2f3a4b5c6d",
  "status": "Pending",
  "totalAmount": 2399.98,
  "items": [
    {
      "id": "c1c2c3c4-1234-5678-abcd-1e2f3a4b5c6d",
      "productId": "e2a3b4c5-1234-5678-abcd-1e2f3a4b5c6d",
      "productName": "iPhone 15 Pro Max",
      "quantity": 2,
      "unitPrice": 1199.99,
      "subtotal": 2399.98
    }
  ],
  "createdAt": "2026-06-01T01:30:00Z",
  "updatedAt": null
}
```

---

## 2. Get My Orders

Retrieves a paginated list of historical orders placed by the currently authenticated customer.

```http
GET /api/v1/orders/me
```

*   **Authentication:** Required (`Customer` or `Admin` role)

### Query Parameters

| Parameter | Type | Default | Description |
|---|:---:|:---:|---|
| **`page`** | int | `1` | The 1-based page index to retrieve. |
| **`pageSize`** | int | `20` | The number of orders to return per page. |

### Response (200 OK - PagedResult)
Returns a `PagedResult<OrderResponse>` wrapper sorted by creation timestamp in descending order:
```json
{
  "items": [
    {
      "id": "b1b2b3b4-1234-5678-abcd-9e2f3a4b5c6d",
      "userId": "d5e6f7a8-1234-5678-abcd-1e2f3a4b5c6d",
      "status": "Pending",
      "totalAmount": 2399.98,
      "items": [
        {
          "id": "c1c2c3c4-1234-5678-abcd-1e2f3a4b5c6d",
          "productId": "e2a3b4c5-1234-5678-abcd-1e2f3a4b5c6d",
          "productName": "iPhone 15 Pro Max",
          "quantity": 2,
          "unitPrice": 1199.99,
          "subtotal": 2399.98
        }
      ],
      "createdAt": "2026-06-01T01:30:00Z",
      "updatedAt": null
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

## 3. Get Order by ID

Retrieves details for a specific order by its unique ID.

```http
GET /api/v1/orders/{id}
```

*   **Authentication:** Required (`Customer` or `Admin` role)

### Path Parameters
*   `id` (guid, required): The unique identifier of the target order.

### Authorization Rules
*   **Customer Role:** Can only view orders they placed. Attempting to view another user's order results in an **HTTP 403 Forbidden** error.
*   **Admin Role:** Authorized to retrieve details for any order in the system.

### Response (200 OK)
Returns the full `OrderResponse` object matching the schema shown in section 1.

---

## 4. Update Order Status

Updates the processing stage of an order (Admin only).

```http
PUT /api/v1/orders/{id}/status
```

*   **Authentication:** Required (`Admin` role)
*   **Cache Invalidation:** Increments the report cache version key.

### Path Parameters
*   `id` (guid, required): The unique identifier of the order.

### Request Body (JSON)
```json
{
  "status": "Confirmed"
}
```

### Order Status Lifecycle Flow
Order status transitions are strictly linear and checked sequentially inside a transaction. Backward skips or non-sequential updates are rejected:

```
Pending ──> Confirmed ──> Shipped ──> Delivered
```

*   **`Pending` ──> `Confirmed`**: Transitions the order from draft to locked.
*   **`Confirmed` ──> `Shipped`**: Marks items as handed over to carrier.
*   **`Shipped` ──> `Delivered`**: Finalized delivery.
*   **`Cancelled`**: This endpoint cannot be used to cancel an order (use the dedicated cancel endpoint instead).

### Response (204 No Content)
Returns an empty response indicating success.

---

## 5. Cancel Order

Cancels a pending order, reverting the status and restoring product inventory stock (Owner or Admin).

```http
POST /api/v1/orders/{id}/cancel
```

*   **Authentication:** Required (`Customer` or `Admin` role)
*   **Ownership Check:** Customers can only cancel their own orders. Admins can cancel any order.
*   **Cache Invalidation:** Increments product catalog and admin reports cache version keys.

### Path Parameters
*   `id` (guid, required): The unique identifier of the order.

### Business Rules & Constraints
*   An order can **only be cancelled if its current status is `Pending`**. Once an order has been transition-updated to `Confirmed` or beyond, cancellation is blocked and returns an **HTTP 400 Bad Request** error.
*   Upon successful cancellation, all purchased item stock quantities are restored atomically to the database inventory in a single transaction.

### Response (204 No Content)
Returns an empty response indicating success.
