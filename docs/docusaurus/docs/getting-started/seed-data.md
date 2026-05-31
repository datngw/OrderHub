---
sidebar_position: 4
title: Seed Data
description: Default accounts and test data created on first migration
---

# Seed Data

When the database is first initialized (via migrations or starting up the container stack), OrderHub automatically seeds test data to facilitate development and performance testing.

## Default User Accounts

The seeder creates two default user accounts with pre-hashed passwords:

| Role | Seed Email | Seed Password | System Access Level |
|---|---|---|---|
| **Admin** | `admin@orderhub.com` | `Admin@123` | Full access. Authorized to manage the product catalog, update order statuses, and view daily sales analytics reports. |
| **Customer** | `customer@orderhub.com` | `User@123` | Standard customer access. Authorized to browse products, place orders, and view/cancel their own pending checkouts. |

### Role Permissions Matrix

The following matrix outlines the permissions of each user role across the API routes:

| Action | API Route Group | Admin | Customer | Anonymous |
|---|---|:---:|:---:|:---:|
| **Browse Product Catalog** | `GET /api/v1/products` | ✅ | ✅ | ✅ |
| **Manage Catalog (CRUD)** | `POST / PUT / DELETE /products` | ✅ | ❌ | ❌ |
| **Checkout (Create Order)** | `POST /api/v1/orders` | ❌ | ✅ | ❌ |
| **View Own Orders** | `GET /api/v1/orders/me` | ❌ | ✅ | ❌ |
| **View Any Order Details** | `GET /api/v1/orders/{id}` | ✅ | ⚠️ (Own only) | ❌ |
| **Update Order Status** | `PUT /api/v1/orders/{id}/status` | ✅ | ❌ | ❌ |
| **Cancel Order** | `POST /api/v1/orders/{id}/cancel` | ✅ | ⚠️ (Own & Pending) | ❌ |
| **View Admin Reports** | `GET /api/v1/admin/reports/...` | ✅ | ❌ | ❌ |

---

## The 10,000 Product Catalog Seeder

To test search optimization, index correctness, and caching behaviors under realistic load, the seeder generates a catalog of **10,000 products** on database initialization.

### Product Distribution Details
*   **Categories:** Seeded products are distributed evenly across **10 categories**:
    *   *Electronics, Clothing, Books, Home & Garden, Sports, Toys, Food, Automotive, Health, Music*.
*   **Name Templating:** Uses realistic product naming templates (e.g., `iPhone {0} Pro Max`, `Samsung Galaxy S{0}`, `MacBook Air M{0}`) matching their categories.
*   **SKU Format:** Unique alphanumeric identifiers generated sequentially from `SKU-00001` to `SKU-10000`.
*   **Pricing:** Randomized price distributions between `$9.99` and `$1,999.99` rounded to 2 decimal places.
*   **Stock Levels:** Randomized inventory stock ranges between `0` and `500` units.

### Memory Optimization Strategy
Because inserting 10,000 records at once can consume significant memory and slow down application startup, the seeder uses a **batching strategy**:
1.  Iterates and instantiates product models.
2.  Adds records to the `DbContext` tracking graph and commits them in batches of **2,000 records** via `SaveChanges()`.
3.  Clears the local lists to release RAM before processing the next batch.

---

## Customizing Seed Data

:::warning
Change seed credentials and emails before deploying to any shared staging or production environments.
:::

The data seeder is located in the Infrastructure project:
*   **Path:** `src/OrderHub.Infrastructure/Persistence/Seed/DataSeeder.cs`

### To modify or regenerate seed data:
1.  Open `DataSeeder.cs` and edit the template lists, account emails, or total product counts.
2.  Drop your existing database or database volume (`docker-compose down -v`).
3.  Re-run the application startup migrations. The hosted service will detect an empty database and execute the modified seeder.
