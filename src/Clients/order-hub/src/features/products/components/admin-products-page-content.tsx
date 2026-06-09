"use client";

import { useState, useCallback } from "react";
import { Plus, RefreshCw, Package } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { ProductPagination } from "./product-pagination";
import { AdminProductFilters } from "./admin-product-filters";
import { AdminProductTable } from "./admin-product-table";
import { ProductForm } from "./product-form";
import { useAdminProducts, useProductMutations } from "../hooks";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";
import type { Product, ProductListParams, CreateProductRequest, UpdateProductRequest } from "../types";

export function AdminProductsPageContent() {
  const [filters, setFilters] = useState<ProductListParams>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });

  // Dialog states
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [productToDelete, setProductToDelete] = useState<Product | null>(null);

  const { data, isLoading, error, refetch } = useAdminProducts(filters);
  const {
    createProduct,
    updateProduct,
    deleteProduct,
    toggleActive,
    isMutating,
  } = useProductMutations(refetch);

  const handleFilterChange = useCallback(
    (newFilters: ProductListParams) => {
      setFilters((prev) => ({
        ...prev,
        ...newFilters,
        page: 1,
      }));
    },
    []
  );

  const handlePageChange = useCallback((page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  }, []);

  const handleCreateClick = useCallback(() => {
    setIsCreateOpen(true);
  }, []);

  const handleEditClick = useCallback((product: Product) => {
    setSelectedProduct(product);
    setIsEditOpen(true);
  }, []);

  const handleDeleteClick = useCallback((product: Product) => {
    setProductToDelete(product);
    setIsDeleteOpen(true);
  }, []);

  const handleToggleActive = useCallback(
    (product: Product) => {
      toggleActive(product.id, product.isActive);
    },
    [toggleActive]
  );

  const handleCreateSubmit = useCallback(
    async (data: CreateProductRequest | UpdateProductRequest) => {
      await createProduct(data as CreateProductRequest);
      setIsCreateOpen(false);
    },
    [createProduct]
  );

  const handleEditSubmit = useCallback(
    async (data: CreateProductRequest | UpdateProductRequest) => {
      if (!selectedProduct) return;
      await updateProduct(selectedProduct.id, data as UpdateProductRequest);
      setIsEditOpen(false);
      setSelectedProduct(null);
    },
    [updateProduct, selectedProduct]
  );

  const handleDeleteConfirm = useCallback(async () => {
    if (!productToDelete) return;
    try {
      await deleteProduct(productToDelete.id);
    } catch {
      // Error handled in mutation hook
    }
    setIsDeleteOpen(false);
    setProductToDelete(null);
  }, [deleteProduct, productToDelete]);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Products"
        description="Manage your product catalog"
        action={
          <Button onClick={handleCreateClick} className="cursor-pointer">
            <Plus className="size-4" />
            Add Product
          </Button>
        }
      />

      {error ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="mb-4 text-destructive">{error}</p>
          <Button variant="outline" onClick={() => refetch()}>
            <RefreshCw className="mr-2 size-4" />
            Try Again
          </Button>
        </div>
      ) : (
        <>
          <AdminProductFilters
            currentFilters={filters}
            onFilterChange={handleFilterChange}
          />

          {!isLoading && data?.items.length === 0 ? (
            <EmptyState
              icon={<Package className="size-10" />}
              title="No products found"
              description="Try adjusting your filters or add a new product"
              action={
                <Button onClick={handleCreateClick} className="cursor-pointer">
                  <Plus className="size-4" />
                  Add Product
                </Button>
              }
            />
          ) : (
            <AdminProductTable
              products={data?.items ?? []}
              isLoading={isLoading}
              onEdit={handleEditClick}
              onDelete={handleDeleteClick}
              onToggleActive={handleToggleActive}
            />
          )}

          {data && data.totalPages > 1 && (
            <ProductPagination
              currentPage={data.page}
              totalPages={data.totalPages}
              onPageChange={handlePageChange}
            />
          )}
        </>
      )}

      {/* Create Dialog */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Create Product</DialogTitle>
            <DialogDescription>
              Add a new product to your catalog
            </DialogDescription>
          </DialogHeader>
          <ProductForm
            onSubmit={handleCreateSubmit}
            onCancel={() => setIsCreateOpen(false)}
            isLoading={isMutating}
          />
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog
        open={isEditOpen}
        onOpenChange={(open) => {
          if (!open) {
            setIsEditOpen(false);
            setSelectedProduct(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Edit Product</DialogTitle>
            <DialogDescription>Update product details</DialogDescription>
          </DialogHeader>
          {selectedProduct && (
            <ProductForm
              product={selectedProduct}
              onSubmit={handleEditSubmit}
              onCancel={() => {
                setIsEditOpen(false);
                setSelectedProduct(null);
              }}
              isLoading={isMutating}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation */}
      <ConfirmDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Product"
        description={`Are you sure you want to delete "${productToDelete?.name ?? ""}"? This action cannot be undone.`}
        confirmLabel="Delete"
        variant="destructive"
        onConfirm={handleDeleteConfirm}
      />
    </div>
  );
}
