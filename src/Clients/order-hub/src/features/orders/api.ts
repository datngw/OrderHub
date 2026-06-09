import { clientFetch } from "@/lib/client-fetch";
import type { PagedResult } from "@/types/common";
import type { Order, OrderListParams, CreateOrderRequest, UpdateOrderStatusRequest } from "./types";

export async function getOrders(params: OrderListParams = {}): Promise<PagedResult<Order>> {
  const query = new URLSearchParams();
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.status) query.set("status", params.status);
  if (params.search) query.set("search", params.search);
  if (params.fromDate) query.set("fromDate", params.fromDate);
  if (params.toDate) query.set("toDate", params.toDate);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortOrder) query.set("sortOrder", params.sortOrder);

  return clientFetch<PagedResult<Order>>(`/api/orders?${query}`);
}

export async function getMyOrders(params: OrderListParams = {}): Promise<PagedResult<Order>> {
  const query = new URLSearchParams();
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.status) query.set("status", params.status);
  if (params.search) query.set("search", params.search);
  if (params.fromDate) query.set("fromDate", params.fromDate);
  if (params.toDate) query.set("toDate", params.toDate);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortOrder) query.set("sortOrder", params.sortOrder);

  return clientFetch<PagedResult<Order>>(`/api/orders/me?${query}`);
}

export async function getOrder(id: string): Promise<Order> {
  return clientFetch<Order>(`/api/orders/${id}`);
}

export async function createOrder(data: CreateOrderRequest): Promise<Order> {
  return clientFetch<Order>("/api/orders", {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function updateOrderStatus(id: string, data: UpdateOrderStatusRequest): Promise<void> {
  return clientFetch<void>(`/api/orders/${id}/status`, {
    method: "PUT",
    body: JSON.stringify(data),
  });
}

export async function cancelOrder(id: string): Promise<void> {
  return clientFetch<void>(`/api/orders/${id}/cancel`, {
    method: "POST",
  });
}

export async function deleteOrder(id: string): Promise<void> {
  return clientFetch<void>(`/api/orders/${id}`, {
    method: "DELETE",
  });
}
