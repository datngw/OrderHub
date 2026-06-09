import { clientFetch } from "@/lib/client-fetch";
import type { PagedResult } from "@/types/common";
import type { Product, ProductListParams, CreateProductRequest, UpdateProductRequest } from "./types";

export async function getProducts(params: ProductListParams = {}): Promise<PagedResult<Product>> {
  const query = new URLSearchParams();
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.search) query.set("search", params.search);
  if (params.category) query.set("category", params.category);
  if (params.minPrice !== undefined) query.set("minPrice", String(params.minPrice));
  if (params.maxPrice !== undefined) query.set("maxPrice", String(params.maxPrice));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortOrder) query.set("sortOrder", params.sortOrder);

  return clientFetch<PagedResult<Product>>(`/api/products?${query}`);
}

export async function getAdminProducts(params: ProductListParams = {}): Promise<PagedResult<Product>> {
  const query = new URLSearchParams();
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params.minPrice !== undefined) query.set("minPrice", String(params.minPrice));
  if (params.maxPrice !== undefined) query.set("maxPrice", String(params.maxPrice));
  if (params.category) query.set("category", params.category);
  if (params.inStock !== undefined) query.set("inStock", String(params.inStock));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortOrder) query.set("sortOrder", params.sortOrder);

  return clientFetch<PagedResult<Product>>(`/api/products?admin=true&${query}`);
}

export async function getProduct(id: string): Promise<Product> {
  return clientFetch<Product>(`/api/products/${id}`);
}

export async function createProduct(data: CreateProductRequest): Promise<Product> {
  return clientFetch<Product>("/api/products", {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function updateProduct(id: string, data: UpdateProductRequest): Promise<Product> {
  return clientFetch<Product>(`/api/products/${id}`, {
    method: "PUT",
    body: JSON.stringify(data),
  });
}

export async function deleteProduct(id: string): Promise<void> {
  return clientFetch<void>(`/api/products/${id}`, {
    method: "DELETE",
  });
}
