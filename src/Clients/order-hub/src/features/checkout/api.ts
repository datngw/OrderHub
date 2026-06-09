import type { Order } from "@/features/orders/types";
import type { CreateOrderRequest } from "./types";

export async function createOrder(data: CreateOrderRequest): Promise<Order> {
  const response = await fetch("/api/orders", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: "Failed to create order" }));
    throw new Error(error.message);
  }
  return response.json();
}
