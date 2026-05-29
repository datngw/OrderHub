---
sidebar_position: 4
title: Orders
description: Order management endpoints — create, view, update status, cancel
---

# Orders

## Create Order

Create a new order with atomic stock deduction. The system calculates the total from current product prices and captures a price snapshot per item.

```
POST /api/v1/orders
```

**Auth:** Customer+ (any authenticated user)

### Request

```json
{
  "items": [
    {
      "productId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "quantity": 2
    },
    {
      "productId": "f0e1d2c3-b4a5-6789-0abc-def123456789",
      "quantity": 1
    }
  ]
}
```

### Validation Rules

| Field | Rules |
|-------|-------|
| Items | Required, at least 1 item |
| ProductId | Required, valid GUID, product must exist and be active |
| Quantity | Required, > 0, must not exceed available stock |

### Processing

1. Begin database transaction
2. Lock all requested product rows (`SELECT ... FOR UPDATE`)
3. Validate stock availability for all items
4. Calculate `TotalAmount` from current product prices
5. Create Order + OrderItems (with `UnitPrice` snapshot)
6. Deduct stock from each product
7. Commit transaction
8. Invalidate product and report caches
9. Return the created order

### Response (201 Created)

```json
{
  "id": "order-guid",
  "userId": "user-guid",
  "status": "Pending",
  "totalAmount": 379.97,
  "createdAt": "2025-06-15T14:30:00Z",
  "items": [
    {
      "productId": "a1b2c3d4-...",
      "productName": "Wireless Headphones",
      "quantity": 2,
      "unitPrice": 149.99
    },
    {
      "productId": "f0e1d2c3-...",
      "productName": "Bluetooth Speaker",
      "quantity": 1,
      "unitPrice": 79.99
    }
  ]
}
```

### Error (409 Conflict)

```json
{
  "type": "...",
  "title": "Business rule violation",
  "status": 409,
  "errors": {
    "Stock": ["Insufficient stock for product 'Widget'. Available: 3, Requested: 5"]
  }
}
```

:::warning
This endpoint uses **pessimistic locking**. Under high contention, requests may wait for locks to release. Verified: 20 concurrent requests / stock=5 → exactly 5 succeed.
:::

---

## Get My Orders

View the authenticated user's order history, paginated.

```
GET /api/v1/orders/me
```

**Auth:** Customer+

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 10 | Items per page |

### Response (200 OK)

Returns `PagedResult<OrderResponse>` with orders sorted by `createdAt` descending.

---

## Get Order by ID

View a single order's details.

```
GET /api/v1/orders/{id}
```

**Auth:** Customer+ (owner or Admin only)

### Authorization

- **Customer** can only view their own orders
- **Admin** can view any order
- Returns 403 if a customer attempts to view another user's order

### Response (200 OK)

Returns the full order with items and price snapshots.

---

## Update Order Status

Transition an order's status (Admin only).

```
PUT /api/v1/orders/{id}/status
```

**Auth:** Admin

### Request

```json
{
  "status": "Confirmed"
}
```

### Valid Transitions

```
Pending → Confirmed → Shipped → Delivered
```

| Transition | Allowed |
|-----------|---------|
| Pending → Confirmed | ✅ |
| Confirmed → Shipped | ✅ |
| Shipped → Delivered | ✅ |
| Any → Cancelled | ❌ (use cancel endpoint) |
| Skipping steps | ❌ |
| Backward transitions | ❌ |

### Response (200 OK)

Returns the updated order with new status.

---

## Cancel Order

Cancel a pending order and restore stock.

```
POST /api/v1/orders/{id}/cancel
```

**Auth:** Customer+ (owner or Admin)

### Rules

- Only orders with status **Pending** can be cancelled
- Stock is restored for all items in the same transaction
- Product and report caches are invalidated

### Response (200 OK)

Returns the order with status `Cancelled`.
