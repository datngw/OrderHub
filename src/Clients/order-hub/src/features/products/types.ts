export interface Product {
  id: string;
  sku: string;
  name: string;
  description: string;
  price: number;
  stock: number;
  category: string;
  mainImageUrl: string;
  galleryImageUrls: string[];
  isActive: boolean;
  createdAt: string;
}

export interface ProductListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
  isActive?: boolean;
}

export interface CreateProductRequest {
  sku: string;
  name: string;
  description: string;
  price: number;
  stock: number;
  category: string;
  mainImageUrl?: string;
  galleryImageUrls?: string[];
  isActive: boolean;
}

export interface UpdateProductRequest {
  name?: string;
  description?: string;
  price?: number;
  stock?: number;
  category?: string;
  mainImageUrl?: string;
  galleryImageUrls?: string[];
  isActive?: boolean;
}
