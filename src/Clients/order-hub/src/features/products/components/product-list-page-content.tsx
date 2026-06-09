"use client";

import { useState, useCallback } from "react";
import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useProducts } from "../hooks";
import type { ProductListParams } from "../types";
import { ProductFilters } from "./product-filters";
import { ProductGrid } from "./product-grid";
import { ProductPagination } from "./product-pagination";

export function ProductListPageContent() {
  const [filters, setFilters] = useState<ProductListParams>({
    page: 1,
    pageSize: 12,
  });

  const { data, isLoading, error, refetch } = useProducts(filters);

  const handleFilterChange = useCallback(
    (newFilters: ProductListParams) => {
      setFilters((prev) => ({
        ...prev,
        ...newFilters,
        page: 1, // Reset to page 1 when filters change
      }));
    },
    []
  );

  const handlePageChange = useCallback((page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  }, []);

  if (error) {
    return (
      <div className="mt-6 flex flex-col items-center justify-center py-16 text-center">
        <p className="mb-4 text-destructive">{error}</p>
        <Button variant="outline" onClick={() => refetch()}>
          <RefreshCw className="mr-2 size-4" /> Try Again
        </Button>
      </div>
    );
  }

  return (
    <div className="mt-6 space-y-6">
      <ProductFilters
        currentFilters={filters}
        onFilterChange={handleFilterChange}
      />
      <ProductGrid
        products={data?.items ?? []}
        isLoading={isLoading}
      />
      {data && (
        <ProductPagination
          currentPage={data.page}
          totalPages={data.totalPages}
          onPageChange={handlePageChange}
        />
      )}
    </div>
  );
}
