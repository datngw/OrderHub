---
sidebar_position: 4
title: Seed Data
description: Default accounts and test data created on first migration
---

# Seed Data

When the database is first created (via migration), OrderHub seeds initial data for development and testing.

## User Accounts

| Role | Email | Password | Purpose |
|------|-------|----------|---------|
| **Admin** | `admin@orderhub.com` | `Admin@123` | Full access: product CRUD, order management, reports |
| **Customer** | `customer@orderhub.com` | `User@123` | Standard access: browse products, create orders |

### Role Capabilities

| Action | Admin | Customer |
|--------|-------|----------|
| Browse products | ✅ | ✅ |
| Create/update/delete products | ✅ | ❌ |
| Create orders | ❌ | ✅ |
| View own orders | ❌ | ✅ |
| View any order | ✅ | ❌ |
| Update order status | ✅ | ❌ |
| Cancel orders | ✅ | ✅ (own, Pending only) |
| View admin reports | ✅ | ❌ |

## Product Catalog

100 products are seeded across 5 categories:

| Category | Products |
|----------|----------|
| Electronics | 20 products |
| Clothing | 20 products |
| Home & Kitchen | 20 products |
| Sports | 20 products |
| Books | 20 products |

Each product has:
- Unique SKU (format: `SKU-{category-prefix}-{number}`)
- Random stock quantity (10–500)
- Prices between $5.00 and $999.99
- Both active and inactive products included

## Customizing Seed Data

:::warning
Change seed account credentials before deploying to any shared or production environment.
:::

Seed data is configured in the Infrastructure layer's seed migration. To modify:

1. Update the seed configuration in `src/OrderHub.Infrastructure/Persistence/Seed/`
2. Remove the existing database or apply a new migration
3. Re-run the application to regenerate seed data
