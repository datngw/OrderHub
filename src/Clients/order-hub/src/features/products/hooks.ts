"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import type { Product, ProductListParams, CreateProductRequest, UpdateProductRequest } from "./types";
import * as productApi from "./api";

export function useProducts(params?: ProductListParams) {
  const query = useQuery({
    queryKey: ["products", params],
    queryFn: () => productApi.getProducts(params),
  });

  return {
    data: query.data ?? null,
    isLoading: query.isLoading,
    error: query.error instanceof Error ? query.error.message : null,
    refetch: query.refetch,
  };
}

export function useProduct(id: string) {
  const query = useQuery({
    queryKey: ["product", id],
    queryFn: () => productApi.getProduct(id),
    enabled: !!id,
  });

  return {
    product: query.data ?? null,
    isLoading: query.isLoading,
    error: query.error instanceof Error ? query.error.message : null,
    refetch: query.refetch,
  };
}

export function useAdminProducts(params?: ProductListParams) {
  const query = useQuery({
    queryKey: ["admin-products", params],
    queryFn: () => productApi.getAdminProducts(params),
  });

  return {
    data: query.data ?? null,
    isLoading: query.isLoading,
    error: query.error instanceof Error ? query.error.message : null,
    refetch: query.refetch,
  };
}

export function useProductMutations(refetch?: () => Promise<unknown>) {
  const queryClient = useQueryClient();

  const createMutation = useMutation({
    mutationFn: (data: CreateProductRequest) => productApi.createProduct(data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["products"] });
      await queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      if (refetch) await refetch();
      toast.success("Product created successfully");
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : "Failed to create product");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProductRequest }) =>
      productApi.updateProduct(id, data),
    onSuccess: async (_, variables) => {
      await queryClient.invalidateQueries({ queryKey: ["products"] });
      await queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      await queryClient.invalidateQueries({ queryKey: ["product", variables.id] });
      if (refetch) await refetch();
      toast.success("Product updated successfully");
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : "Failed to update product");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => productApi.deleteProduct(id),
    onSuccess: async (_, id) => {
      await queryClient.invalidateQueries({ queryKey: ["products"] });
      await queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      await queryClient.invalidateQueries({ queryKey: ["product", id] });
      if (refetch) await refetch();
      toast.success("Product deleted successfully");
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : "Failed to delete product");
    },
  });

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, currentIsActive }: { id: string; currentIsActive: boolean }) =>
      productApi.updateProduct(id, { isActive: !currentIsActive }),
    onSuccess: async (_, variables) => {
      await queryClient.invalidateQueries({ queryKey: ["products"] });
      await queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      await queryClient.invalidateQueries({ queryKey: ["product", variables.id] });
      if (refetch) await refetch();
      toast.success(variables.currentIsActive ? "Product deactivated" : "Product activated");
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : "Failed to update product status");
    },
  });

  return {
    createProduct: (data: CreateProductRequest) => createMutation.mutateAsync(data),
    updateProduct: (id: string, data: UpdateProductRequest) => updateMutation.mutateAsync({ id, data }),
    deleteProduct: (id: string) => deleteMutation.mutateAsync(id),
    toggleActive: (id: string, currentIsActive: boolean) => toggleActiveMutation.mutateAsync({ id, currentIsActive }),
    isMutating:
      createMutation.isPending ||
      updateMutation.isPending ||
      deleteMutation.isPending ||
      toggleActiveMutation.isPending,
    mutationError:
      (createMutation.error instanceof Error ? createMutation.error.message : null) ||
      (updateMutation.error instanceof Error ? updateMutation.error.message : null) ||
      (deleteMutation.error instanceof Error ? deleteMutation.error.message : null) ||
      (toggleActiveMutation.error instanceof Error ? toggleActiveMutation.error.message : null),
  };
}
