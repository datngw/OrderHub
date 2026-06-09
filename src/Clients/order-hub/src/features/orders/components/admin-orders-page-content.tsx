"use client";

import { useState, useCallback } from "react";
import { RefreshCw, ShoppingBag } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { ROUTES, DEFAULT_PAGE_SIZE } from "@/lib/constants";
import { useOrders, useOrderMutations } from "../hooks";
import { AdminOrderFilters } from "./admin-order-filters";
import { AdminOrderTable } from "./admin-order-table";
import { OrderListPagination } from "./order-list-pagination";
import type { Order, OrderListParams } from "../types";

export function AdminOrdersPageContent() {
  const [filters, setFilters] = useState<OrderListParams>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });

  // Dialog states
  const [isCancelOpen, setIsCancelOpen] = useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);

  const { data, isLoading, error, refetch } = useOrders(filters);
  const { updateStatus, cancelOrder, deleteOrder, isMutating } =
    useOrderMutations(refetch);

  const handleFilterChange = useCallback(
    (newFilters: OrderListParams) => {
      setFilters((prev) => ({
        ...prev,
        ...newFilters,
        page: newFilters.page ?? 1,
      }));
    },
    []
  );

  const handlePageChange = useCallback((page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  }, []);

  const handleViewDetail = useCallback((order: Order) => {
    window.location.href = ROUTES.adminOrderDetail(order.id);
  }, []);

  const handleQuickStatusUpdate = useCallback(
    (order: Order, newStatus: string) => {
      updateStatus(order.id, newStatus);
    },
    [updateStatus]
  );

  const handleCancelOrder = useCallback((order: Order) => {
    setSelectedOrder(order);
    setIsCancelOpen(true);
  }, []);

  const handleDeleteOrder = useCallback((order: Order) => {
    setSelectedOrder(order);
    setIsDeleteOpen(true);
  }, []);

  const handleCancelConfirm = useCallback(async () => {
    if (!selectedOrder) return;
    try {
      await cancelOrder(selectedOrder.id);
      setIsCancelOpen(false);
      setSelectedOrder(null);
    } catch {
      // Error handled in hook
    }
  }, [cancelOrder, selectedOrder]);

  const handleDeleteConfirm = useCallback(async () => {
    if (!selectedOrder) return;
    try {
      await deleteOrder(selectedOrder.id);
      setIsDeleteOpen(false);
      setSelectedOrder(null);
    } catch {
      // Error handled in hook
    }
  }, [deleteOrder, selectedOrder]);

  return (
    <div className="space-y-6">
      <PageHeader title="Orders" description="Manage all customer orders" />

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
          <AdminOrderFilters
            currentFilters={filters}
            onFilterChange={handleFilterChange}
          />

          {!isLoading && data?.items.length === 0 ? (
            <EmptyState
              icon={<ShoppingBag className="size-10" />}
              title="No orders found"
              description="Try adjusting your filters or wait for new orders"
            />
          ) : (
            <AdminOrderTable
              orders={data?.items ?? []}
              isLoading={isLoading}
              onViewDetail={handleViewDetail}
              onQuickStatusUpdate={handleQuickStatusUpdate}
              onCancelOrder={handleCancelOrder}
              onDeleteOrder={handleDeleteOrder}
            />
          )}

          {data && data.totalPages > 1 && (
            <OrderListPagination
              currentPage={data.page}
              totalPages={data.totalPages}
              onPageChange={handlePageChange}
            />
          )}
        </>
      )}

      {/* Cancel Confirmation */}
      <ConfirmDialog
        open={isCancelOpen}
        onOpenChange={(open) => {
          setIsCancelOpen(open);
          if (!open) setSelectedOrder(null);
        }}
        title="Cancel Order"
        description={`Are you sure you want to cancel order #${selectedOrder?.id.slice(-8).toUpperCase() ?? ""}? This will restore the stock for all items.`}
        confirmLabel={isMutating ? "Cancelling..." : "Cancel Order"}
        cancelLabel="Keep Order"
        variant="destructive"
        onConfirm={handleCancelConfirm}
      />

      {/* Delete Confirmation */}
      <ConfirmDialog
        open={isDeleteOpen}
        onOpenChange={(open) => {
          setIsDeleteOpen(open);
          if (!open) setSelectedOrder(null);
        }}
        title="Delete Order"
        description={`Are you sure you want to permanently delete order #${selectedOrder?.id.slice(-8).toUpperCase() ?? ""}? This action cannot be undone.`}
        confirmLabel={isMutating ? "Deleting..." : "Delete"}
        variant="destructive"
        onConfirm={handleDeleteConfirm}
      />
    </div>
  );
}
