"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import type { Order, OrderListParams } from "./types";
import * as orderApi from "./api";

export function useOrders(params?: OrderListParams) {
  const query = useQuery({
    queryKey: ["orders", params],
    queryFn: () => orderApi.getOrders(params),
  });

  return {
    data: query.data ?? null,
    isLoading: query.isLoading,
    error: query.error instanceof Error ? query.error.message : null,
    refetch: query.refetch,
  };
}

export function useMyOrders(params?: OrderListParams) {
  const query = useQuery({
    queryKey: ["my-orders", params],
    queryFn: () => orderApi.getMyOrders(params),
  });

  return {
    data: query.data ?? null,
    isLoading: query.isLoading,
    error: query.error instanceof Error ? query.error.message : null,
    refetch: query.refetch,
  };
}

export function useOrder(id: string) {
  const query = useQuery({
    queryKey: ["order", id],
    queryFn: () => orderApi.getOrder(id),
    enabled: !!id,
  });

  return {
    order: query.data ?? null,
    isLoading: query.isLoading,
    error: query.error instanceof Error ? query.error.message : null,
    refetch: query.refetch,
  };
}

export function useOrderMutations(refetch?: () => Promise<unknown>) {
  const queryClient = useQueryClient();

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      orderApi.updateOrderStatus(id, { status }),
    onSuccess: async (_, variables) => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
      await queryClient.invalidateQueries({ queryKey: ["my-orders"] });
      await queryClient.invalidateQueries({ queryKey: ["order", variables.id] });
      if (refetch) await refetch();
      toast.success("Order status updated successfully");
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : "Failed to update order status");
    },
  });

  const cancelOrderMutation = useMutation({
    mutationFn: (id: string) => orderApi.cancelOrder(id),
    onSuccess: async (_, id) => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
      await queryClient.invalidateQueries({ queryKey: ["my-orders"] });
      await queryClient.invalidateQueries({ queryKey: ["order", id] });
      if (refetch) await refetch();
      toast.success("Order cancelled successfully");
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : "Failed to cancel order");
    },
  });

  const deleteOrderMutation = useMutation({
    mutationFn: (id: string) => orderApi.deleteOrder(id),
    onSuccess: async (_, id) => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
      await queryClient.invalidateQueries({ queryKey: ["my-orders"] });
      await queryClient.invalidateQueries({ queryKey: ["order", id] });
      if (refetch) await refetch();
      toast.success("Order deleted successfully");
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : "Failed to delete order");
    },
  });

  return {
    updateStatus: (id: string, status: string) => updateStatusMutation.mutateAsync({ id, status }),
    cancelOrder: (id: string) => cancelOrderMutation.mutateAsync(id),
    deleteOrder: (id: string) => deleteOrderMutation.mutateAsync(id),
    isMutating:
      updateStatusMutation.isPending ||
      cancelOrderMutation.isPending ||
      deleteOrderMutation.isPending,
  };
}
