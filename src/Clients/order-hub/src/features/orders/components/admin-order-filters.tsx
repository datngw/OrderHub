"use client";

import { useState, useEffect, useRef } from "react";
import { Search, SlidersHorizontal, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { ORDER_STATUS } from "@/lib/constants";
import type { OrderListParams } from "../types";

interface AdminOrderFiltersProps {
  onFilterChange: (filters: OrderListParams) => void;
  currentFilters: OrderListParams;
}

const STATUS_OPTIONS = [
  { label: "All Statuses", value: "all" },
  { label: "Pending", value: ORDER_STATUS.Pending },
  { label: "Confirmed", value: ORDER_STATUS.Confirmed },
  { label: "Shipped", value: ORDER_STATUS.Shipped },
  { label: "Delivered", value: ORDER_STATUS.Delivered },
  { label: "Cancelled", value: ORDER_STATUS.Cancelled },
] as const;

const SORT_OPTIONS = [
  { label: "Newest", key: "newest", sortBy: "CreatedAt", sortOrder: "desc" as const },
  { label: "Oldest", key: "oldest", sortBy: "CreatedAt", sortOrder: "asc" as const },
  { label: "Total ↑", key: "TotalAmount-asc", sortBy: "TotalAmount", sortOrder: "asc" as const },
  { label: "Total ↓", key: "TotalAmount-desc", sortBy: "TotalAmount", sortOrder: "desc" as const },
];

function countActiveFilters(f: OrderListParams): number {
  let n = 0;
  if (f.status) n++;
  if (f.fromDate) n++;
  if (f.toDate) n++;
  return n;
}

export function AdminOrderFilters({
  onFilterChange,
  currentFilters,
}: AdminOrderFiltersProps) {
  const [expanded, setExpanded] = useState(false);
  const [searchTerm, setSearchTerm] = useState(currentFilters.search ?? "");
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(null);
  const [fromDate, setFromDate] = useState(currentFilters.fromDate ?? "");
  const [toDate, setToDate] = useState(currentFilters.toDate ?? "");

  const activeCount = countActiveFilters(currentFilters);

  // Debounced search
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      onFilterChange({ ...currentFilters, search: searchTerm || undefined });
    }, 300);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm]);

  function handleStatusChange(value: string | null) {
    onFilterChange({
      ...currentFilters,
      status: value === "all" || !value ? undefined : value,
      page: 1,
    });
  }

  function handleSortChange(value: string | null) {
    if (!value) return;
    const option = SORT_OPTIONS.find((o) => o.key === value);
    if (option) {
      onFilterChange({
        ...currentFilters,
        sortBy: option.sortBy,
        sortOrder: option.sortOrder,
        page: 1,
      });
    }
  }

  function handleFromDateChange(value: string) {
    setFromDate(value);
    onFilterChange({
      ...currentFilters,
      fromDate: value || undefined,
      page: 1,
    });
  }

  function handleToDateChange(value: string) {
    setToDate(value);
    onFilterChange({
      ...currentFilters,
      toDate: value || undefined,
      page: 1,
    });
  }

  function clearAll() {
    setSearchTerm("");
    setFromDate("");
    setToDate("");
    onFilterChange({
      page: 1,
      pageSize: currentFilters.pageSize,
      search: undefined,
      status: undefined,
      fromDate: undefined,
      toDate: undefined,
      sortBy: undefined,
      sortOrder: undefined,
    });
  }

  const currentSortKey =
    SORT_OPTIONS.find(
      (o) => o.sortBy === currentFilters.sortBy && o.sortOrder === currentFilters.sortOrder
    )?.key ?? "newest";

  const currentStatus = currentFilters.status ?? "all";

  return (
    <div className="space-y-3">
      {/* Search bar + toggle */}
      <div className="flex items-center gap-2">
        <div className="relative flex-1 sm:max-w-sm">
          <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search by customer, email, order ID..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="h-10 pl-8"
          />
        </div>

        <Button
          variant="outline"
          size="default"
          className="gap-1.5"
          onClick={() => setExpanded(!expanded)}
        >
          <SlidersHorizontal className="size-4" />
          Filters
          {activeCount > 0 && (
            <span className="flex size-5 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">
              {activeCount}
            </span>
          )}
        </Button>

        <Select value={currentSortKey} onValueChange={handleSortChange}>
          <SelectTrigger className="h-10 w-[150px]">
            {SORT_OPTIONS.find((o) => o.key === currentSortKey)?.label ?? "Newest"}
          </SelectTrigger>
          <SelectContent>
            {SORT_OPTIONS.map((o) => (
              <SelectItem key={o.key} value={o.key}>
                {o.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {activeCount > 0 && (
          <Button variant="ghost" size="sm" className="text-muted-foreground" onClick={clearAll}>
            <X className="size-3.5" />
            Clear all
          </Button>
        )}
      </div>

      {/* Expandable filters */}
      {expanded && (
        <div className="flex flex-wrap items-center gap-3 rounded-lg border bg-muted/30 p-3">
          {/* Status */}
          <div className="flex items-center gap-1.5">
            <span className="text-sm text-muted-foreground whitespace-nowrap">Status</span>
            <Select value={currentStatus} onValueChange={handleStatusChange}>
              <SelectTrigger className="h-9 w-[150px] text-sm">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {STATUS_OPTIONS.map((o) => (
                  <SelectItem key={o.value} value={o.value}>
                    {o.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Date range */}
          <div className="flex items-center gap-1.5">
            <span className="text-sm text-muted-foreground whitespace-nowrap">From</span>
            <Input
              type="date"
              value={fromDate}
              onChange={(e) => handleFromDateChange(e.target.value)}
              className="h-9 w-[150px] text-sm"
            />
          </div>

          <div className="flex items-center gap-1.5">
            <span className="text-sm text-muted-foreground whitespace-nowrap">To</span>
            <Input
              type="date"
              value={toDate}
              onChange={(e) => handleToDateChange(e.target.value)}
              className="h-9 w-[150px] text-sm"
            />
          </div>
        </div>
      )}
    </div>
  );
}
