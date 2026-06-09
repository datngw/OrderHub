"use client";

import { useState, useCallback, useMemo } from "react";
import { RefreshCw, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ORDER_STATUS, type OrderStatus } from "@/lib/constants";
import { useMyOrders } from "../hooks";
import type { OrderListParams } from "../types";
import { OrderFilters } from "./order-filters";
import { OrderList } from "./order-list";
import { OrderListPagination } from "./order-list-pagination";

const STATUS_CONFIG: { value: OrderStatus; label: string; activeBg: string }[] = [
  { value: ORDER_STATUS.Pending,    label: "Pending",    activeBg: "bg-yellow-50 border-yellow-300 text-yellow-800" },
  { value: ORDER_STATUS.Confirmed,  label: "Confirmed",  activeBg: "bg-blue-50 border-blue-300 text-blue-800" },
  { value: ORDER_STATUS.Shipped,    label: "Shipped",    activeBg: "bg-purple-50 border-purple-300 text-purple-800" },
  { value: ORDER_STATUS.Delivered,  label: "Delivered",  activeBg: "bg-green-50 border-green-300 text-green-800" },
  { value: ORDER_STATUS.Cancelled,  label: "Cancelled",  activeBg: "bg-red-50 border-red-300 text-red-800" },
];

export function MyOrdersPageContent() {
  const [selectedStatuses, setSelectedStatuses] = useState<OrderStatus[]>([]);
  const [filters, setFilters] = useState<OrderListParams>({
    page: 1,
    pageSize: 10,
    sortBy: "CreatedAt",
    sortOrder: "desc",
  });

  const toggleStatus = useCallback((status: OrderStatus) => {
    setSelectedStatuses((prev) =>
      prev.includes(status) ? prev.filter((s) => s !== status) : [...prev, status],
    );
    setFilters((prev) => ({ ...prev, page: 1 }));
  }, []);

  const clearStatuses = useCallback(() => {
    setSelectedStatuses([]);
    setFilters((prev) => ({ ...prev, page: 1 }));
  }, []);

  // Build API params: single status -> server-side filter; multiple/none -> fetch all, filter client-side
  const params = useMemo<OrderListParams>(() => {
    if (selectedStatuses.length === 1) {
      return { ...filters, status: selectedStatuses[0] };
    }
    return { ...filters, status: undefined };
  }, [filters, selectedStatuses]);

  const { data, isLoading, error, refetch } = useMyOrders(params);

  // Client-side filter when multiple statuses selected
  const filteredOrders = useMemo(() => {
    const items = data?.items ?? [];
    if (selectedStatuses.length <= 1) return items;
    return items.filter((order) => selectedStatuses.includes(order.status as OrderStatus));
  }, [data?.items, selectedStatuses]);

  const handleFilterChange = useCallback(
    (partial: Partial<OrderListParams>) => {
      setFilters((prev) => ({ ...prev, ...partial }));
    },
    [],
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
      {/* Status Filter — multi-select chips with colored dots */}
      <div className="flex flex-wrap items-center gap-2">
        {STATUS_CONFIG.map(({ value, label, activeBg }) => {
          const active = selectedStatuses.includes(value);
          return (
            <button
              key={value}
              type="button"
              onClick={() => toggleStatus(value)}
              className={`inline-flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-medium transition-all ${
                active
                  ? activeBg
                  : "border-border bg-background text-muted-foreground hover:bg-muted"
              }`}
            >
              {label}
              {active && (
                <X className="ml-0.5 size-3 opacity-60" />
              )}
            </button>
          );
        })}
        {selectedStatuses.length > 0 && (
          <button
            type="button"
            onClick={clearStatuses}
            className="inline-flex items-center rounded-full border border-border px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:bg-muted"
          >
            Clear all
          </button>
        )}
      </div>

      {/* Filters */}
      <OrderFilters onFilterChange={handleFilterChange} />

      {/* Order List */}
      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <RefreshCw className="size-6 animate-spin text-muted-foreground" />
        </div>
      ) : (
        <>
          <OrderList orders={filteredOrders} />
          {data && (
            <OrderListPagination
              currentPage={data.page}
              totalPages={data.totalPages}
              onPageChange={handlePageChange}
            />
          )}
        </>
      )}
    </div>
  );
}
