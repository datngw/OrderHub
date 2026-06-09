"use client";

import { useCallback } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { Basket } from "./types";
import * as basketApi from "./api";

function saveCartCount(basket: Basket | null) {
  if (typeof window === "undefined") return;
  if (basket && basket.totalItems > 0) {
    sessionStorage.setItem("cart_count", String(basket.totalItems));
  } else {
    sessionStorage.removeItem("cart_count");
  }
  window.dispatchEvent(new Event("cart_count_changed"));
}

export function useBasket() {
  const queryClient = useQueryClient();

  const { data: basket = null, isLoading, refetch } = useQuery({
    queryKey: ["basket"],
    queryFn: async () => {
      try {
        const data = await basketApi.getBasket();
        saveCartCount(data);
        return data;
      } catch (err) {
        saveCartCount(null);
        return null;
      }
    },
    staleTime: Infinity,
    retry: false,
  });

  const fetchBasket = useCallback(async () => {
    await refetch();
  }, [refetch]);

  const addItemMutation = useMutation({
    mutationFn: ({ productId, quantity }: { productId: string; quantity: number }) =>
      basketApi.addBasketItem(productId, quantity),
    onSuccess: (data) => {
      queryClient.setQueryData(["basket"], data);
      saveCartCount(data);
    },
  });

  const updateItemMutation = useMutation({
    mutationFn: ({ productId, quantity }: { productId: string; quantity: number }) =>
      basketApi.updateBasketItem(productId, quantity),
    onSuccess: (data) => {
      queryClient.setQueryData(["basket"], data);
      saveCartCount(data);
    },
  });

  const removeItemMutation = useMutation({
    mutationFn: (productId: string) =>
      basketApi.removeBasketItem(productId),
    onSuccess: (data) => {
      queryClient.setQueryData(["basket"], data);
      saveCartCount(data);
    },
  });

  const clearMutation = useMutation({
    mutationFn: () => basketApi.clearBasket(),
    onSuccess: () => {
      queryClient.setQueryData(["basket"], null);
      saveCartCount(null);
    },
  });

  return {
    basket,
    isLoading:
      isLoading ||
      addItemMutation.isPending ||
      updateItemMutation.isPending ||
      removeItemMutation.isPending ||
      clearMutation.isPending,
    fetchBasket,
    addItem: (productId: string, quantity: number) =>
      addItemMutation.mutateAsync({ productId, quantity }),
    updateItem: (productId: string, quantity: number) =>
      updateItemMutation.mutateAsync({ productId, quantity }),
    removeItem: (productId: string) =>
      removeItemMutation.mutateAsync(productId),
    clear: () =>
      clearMutation.mutateAsync(),
  };
}
