# Plan: Phase 2 — Order Endpoints (P0)

**Architecture:** Clean Architecture (4-layer)
**Affected layers:** Domain, Application, Infrastructure, Api
**Estimated steps:** 8

## Scope

Implement all 5 order endpoints from GOALS.md Phase 2:
1. `POST /api/v1/orders` — Atomic creation with pessimistic locking, price snapshot, stock deduction
2. `GET /api/v1/orders/me` — Current user's order history, paginated
3. `GET /api/v1/orders/{id}` — Owner or admin only
4. `PUT /api/v1/orders/{id}/status` — Admin transitions: Pending → Confirmed → Shipped → Delivered
5. `POST /api/v1/orders/{id}/cancel` — Cancel only if Pending, restore stock

---

## Steps

### Step 1 — Domain: OrderErrors + IOrderRepository
**Layer:** Domain
**Files:**
- `src/OrderHub.Domain/Orders/OrderErrors.cs` — Typed error definitions (NotFound, AlreadyCancelled, InvalidStatusTransition, InsufficientStock, EmptyOrder, ProductNotFound, ProductUnavailable)
- `src/OrderHub.Domain/Orders/IOrderRepository.cs` — Repository interface with methods:
  - `GetByIdAsync` (with items, for detail view)
  - `GetByIdForUpdateAsync` (with `FOR UPDATE` skip locked, for creation flow)
  - `GetOrdersByUserIdAsync` (paginated, for /me)
  - `Add`
  - `Update`
  > No Add on OrderItem — items are added via Order.Items navigation, EF tracks them.

### Step 2 — Application: Response DTOs
**Layer:** Application
**Files:**
- `src/OrderHub.Application/Features/Orders/OrderResponse.cs` — OrderResponse record (Id, UserId, Status, TotalAmount, Items list, CreatedAt, UpdatedAt)
- `src/OrderHub.Application/Features/Orders/OrderItemResponse.cs` — OrderItemResponse record (Id, ProductId, ProductName, Quantity, UnitPrice, Subtotal)

### Step 3 — Application: CreateOrder (CQRS)
**Layer:** Application
**Files:**
- `Features/Orders/CreateOrder/CreateOrderCommand.cs` — record with UserId + List of (ProductId, Quantity) items
- `Features/Orders/CreateOrder/CreateOrderCommandHandler.cs` — Core business logic:
  1. Validate non-empty items list
  2. Fetch products by IDs using `FOR UPDATE SKIP LOCKED` (pessimistic locking)
  3. Validate all products exist and are active
  4. Validate sufficient stock for each
  5. Deduct stock, calculate total (server-side), create Order + OrderItems with price snapshots
  6. Save via UnitOfWork
- `Features/Orders/CreateOrder/CreateOrderCommandValidator.cs` — Non-empty items, valid quantities (> 0)

### Step 4 — Application: GetMyOrders + GetOrderById (CQRS)
**Layer:** Application
**Files:**
- `Features/Orders/GetMyOrders/GetMyOrdersQuery.cs` — record(UserId, Page, PageSize)
- `Features/Orders/GetMyOrders/GetMyOrdersQueryHandler.cs` — Paginated query with items
- `Features/Orders/GetOrderById/GetOrderByIdQuery.cs` — record(OrderId, UserId, IsAdmin)
- `Features/Orders/GetOrderById/GetOrderByIdQueryHandler.cs` — Owner or admin authorization check

### Step 5 — Application: UpdateOrderStatus + CancelOrder (CQRS)
**Layer:** Application
**Files:**
- `Features/Orders/UpdateOrderStatus/UpdateOrderStatusCommand.cs` — record(OrderId, NewStatus)
- `Features/Orders/UpdateOrderStatus/UpdateOrderStatusCommandHandler.cs` — Validate status transition sequence (Pending→Confirmed→Shipped→Delivered), cannot transition from/to Cancelled
- `Features/Orders/UpdateOrderStatus/UpdateOrderStatusCommandValidator.cs` — Valid enum value
- `Features/Orders/CancelOrder/CancelOrderCommand.cs` — record(OrderId, UserId, IsAdmin)
- `Features/Orders/CancelOrder/CancelOrderCommandHandler.cs` — Only if Pending, restore stock for all items in transaction

### Step 6 — Infrastructure: OrderRepository + DI Registration
**Layer:** Infrastructure
**Files:**
- `src/OrderHub.Infrastructure/Persistence/Repositories/OrderRepository.cs` — Implementation with:
  - `GetByIdAsync` — `Include(o => o.Items).ThenInclude(i => i.Product).AsNoTracking()`
  - `GetByIdForUpdateAsync` — `FromSqlRaw` or EF `ForUpdate` extension with pessimistic locking
  - `GetOrdersByUserIdAsync` — Paginated, includes items
  - `Add` / `Update`
- Modify `src/OrderHub.Infrastructure/DependencyInjection.cs` — Register `IOrderRepository`

### Step 7 — API: Request DTOs + Endpoints
**Layer:** Api
**Files:**
- `src/OrderHub.Api/Endpoints/Orders/Requests/CreateOrderRequest.cs` — record with List<OrderItemRequest> (ProductId, Quantity)
- `src/OrderHub.Api/Endpoints/Orders/Requests/UpdateOrderStatusRequest.cs` — record with Status (string/enum)
- `src/OrderHub.Api/Endpoints/Orders/OrderEndpoints.cs` — All 5 endpoints following ProductEndpoints pattern:
  - `/api/v{version:apiVersion}/orders` group, v1, "Orders" tag
  - POST / → CreateOrder (authenticated, any role)
  - GET /me → GetMyOrders (authenticated)
  - GET /{id:guid} → GetOrderById (authenticated, owner/admin check in handler)
  - PUT /{id:guid}/status → UpdateOrderStatus (admin only)
  - POST /{id:guid}/cancel → CancelOrder (authenticated, owner/admin check in handler)

### Step 8 — Build Verification
- `dotnet build OrderHub.slnx` — Ensure everything compiles
- Verify all endpoints are registered correctly

---

## Open Questions

1. **Authorization for /me endpoint:** The handler needs the current user's ID. Should we extract it from `HttpContext.User` in the endpoint and pass it to the command/query, or use a different mechanism?
   → **Plan:** Extract userId from JWT claims in endpoint handler (matching auth pattern), pass to command/query.

2. **Pessimistic locking approach:** EF Core doesn't natively support `FOR UPDATE` on PostgreSQL. Options:
   - a) Raw SQL via `FromSqlRaw` for the locked product query
   - b) Use `Npgsql`'s `ForUpdate()` extension if available in the EF Core Npgsql provider
   → **Plan:** Use `context.Products.FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Id\" = ANY({0}) FOR UPDATE", ids)` or the Npgsql EF Core `ForUpdate()` if available. Will verify during implementation.

---

## Risks

- **Pessimistic locking syntax:** EF Core's built-in support for `FOR UPDATE` is limited. May need raw SQL fallback. Mitigation: implement repository method first, verify it works.
- **Transaction scope for CreateOrder:** Stock deduction + order creation must be atomic. The handler will use `IUnitOfWork.BeginTransactionAsync` + `CommitTransactionAsync` with rollback on failure.
- **Cancel stock restoration:** Must restore stock in the same transaction as order status update. Same pattern as create — explicit transaction via UnitOfWork.
