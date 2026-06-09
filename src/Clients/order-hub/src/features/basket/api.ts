import { clientFetch } from "@/lib/client-fetch";
import type { Basket } from "./types";

export async function getBasket(): Promise<Basket> {
  return clientFetch<Basket>("/api/basket");
}

export async function addBasketItem(productId: string, quantity: number): Promise<Basket> {
  return clientFetch<Basket>("/api/basket/items", {
    method: "POST",
    body: JSON.stringify({ productId, quantity }),
  });
}

export async function updateBasketItem(productId: string, quantity: number): Promise<Basket> {
  return clientFetch<Basket>(`/api/basket/items/${productId}`, {
    method: "PUT",
    body: JSON.stringify({ quantity }),
  });
}

export async function removeBasketItem(productId: string): Promise<Basket> {
  return clientFetch<Basket>(`/api/basket/items/${productId}`, {
    method: "DELETE",
  });
}

export async function clearBasket(): Promise<void> {
  return clientFetch<void>("/api/basket", { method: "DELETE" });
}
