"use client";

import { useState, useEffect, useRef } from "react";
import { Search, SlidersHorizontal, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { formatNumberInput, parseFormattedNumber } from "@/lib/utils";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { ProductListParams } from "../types";

interface AdminProductFiltersProps {
  onFilterChange: (filters: ProductListParams) => void;
  currentFilters: ProductListParams;
}

const CATEGORIES = [
  "Electronics",
  "Clothing",
  "Home & Garden",
  "Sports",
  "Books",
  "Toys",
  "Food & Beverage",
  "Health & Beauty",
];

const STOCK_OPTIONS = [
  { label: "All Stock", value: "all" },
  { label: "In Stock", value: "in-stock" },
  { label: "Out of Stock", value: "out-of-stock" }
] as const;

const SORT_OPTIONS = [
  { label: "Newest", key: "CreatedAt-desc", sortBy: "CreatedAt", sortOrder: "desc" as const },
  { label: "Name A-Z", key: "Name-asc", sortBy: "Name", sortOrder: "asc" as const },
  { label: "Name Z-A", key: "Name-desc", sortBy: "Name", sortOrder: "desc" as const },
  { label: "Price ↑", key: "Price-asc", sortBy: "Price", sortOrder: "asc" as const },
  { label: "Price ↓", key: "Price-desc", sortBy: "Price", sortOrder: "desc" as const },
  { label: "Stock ↑", key: "Stock-asc", sortBy: "Stock", sortOrder: "asc" as const },
  { label: "Stock ↓", key: "Stock-desc", sortBy: "Stock", sortOrder: "desc" as const },
];

function countActiveFilters(f: ProductListParams): number {
  let n = 0;
  if (f.category) n++;
  if (f.isActive !== undefined) n++;
  if (f.inStock !== undefined) n++;
  if (f.minPrice !== undefined || f.maxPrice !== undefined) n++;
  return n;
}

export function AdminProductFilters({
  onFilterChange,
  currentFilters,
}: AdminProductFiltersProps) {
  const [expanded, setExpanded] = useState(false);
  const [searchTerm, setSearchTerm] = useState(currentFilters.search ?? "");
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(null);
  const [minPrice, setMinPrice] = useState(
    currentFilters.minPrice != null
      ? formatNumberInput(currentFilters.minPrice.toString())
      : ""
  );
  const [maxPrice, setMaxPrice] = useState(
    currentFilters.maxPrice != null
      ? formatNumberInput(currentFilters.maxPrice.toString())
      : ""
  );

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

  function commitPrice() {
    const min = parseFormattedNumber(minPrice);
    const max = parseFormattedNumber(maxPrice);
    onFilterChange({
      ...currentFilters,
      minPrice: min ? Number(min) : undefined,
      maxPrice: max ? Number(max) : undefined,
    });
  }

  function clearPrice() {
    setMinPrice("");
    setMaxPrice("");
    onFilterChange({ ...currentFilters, minPrice: undefined, maxPrice: undefined });
  }

  function handleStockChange(value: string | null) {
    const inStock = value === "in-stock" ? true : value === "out-of-stock" ? false : undefined;
    onFilterChange({ ...currentFilters, inStock });
  }

  function handleSortChange(value: string | null) {
    if (value === null) return;
    const o = SORT_OPTIONS.find((s) => s.key === value);
    if (o) onFilterChange({ ...currentFilters, sortBy: o.sortBy, sortOrder: o.sortOrder });
  }

  function clearAll() {
    setSearchTerm("");
    setMinPrice("");
    setMaxPrice("");
    onFilterChange({
      page: 1,
      pageSize: currentFilters.pageSize,
      search: undefined,
      category: undefined,
      isActive: undefined,
      minPrice: undefined,
      maxPrice: undefined,
      inStock: undefined,
      sortBy: undefined,
      sortOrder: undefined,
    });
  }

  const currentSortKey = SORT_OPTIONS.find(
    (o) => o.sortBy === currentFilters.sortBy && o.sortOrder === currentFilters.sortOrder
  )?.key ?? "CreatedAt-desc";

  const currentStatus = currentFilters.isActive === true ? "active"
    : currentFilters.isActive === false ? "inactive" : "all";

  const currentStock = currentFilters.inStock === true ? "in-stock"
    : currentFilters.inStock === false ? "out-of-stock" : "all";

  return (
    <div className="space-y-3">
      {/* Search bar + toggle */}
      <div className="flex items-center gap-2">
        <div className="relative flex-1 sm:max-w-sm">
          <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search by name or SKU..."
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
              <SelectItem key={o.key} value={o.key}>{o.label}</SelectItem>
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
          {/* Category */}
          <div className="flex items-center gap-1.5">
            <span className="text-sm text-muted-foreground whitespace-nowrap">Category</span>
            <Select
              value={currentFilters.category ?? ""}
              onValueChange={(v) => onFilterChange({ ...currentFilters, category: v || undefined })}
            >
              <SelectTrigger className="h-9 w-[150px] text-sm">
                <SelectValue placeholder="All" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="" label="All">All</SelectItem>
                {CATEGORIES.map((c) => <SelectItem key={c} value={c} label={c}>{c}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>

          {/* Status */}
          <div className="flex items-center gap-1.5">
            <span className="text-sm text-muted-foreground whitespace-nowrap">Status</span>
            <Select
              value={currentStatus}
              onValueChange={(v) => onFilterChange({ ...currentFilters, isActive: v === "active" ? true : v === "inactive" ? false : undefined })}
            >
              <SelectTrigger className="h-9 w-[120px] text-sm">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" label="All">All</SelectItem>
                <SelectItem value="active" label="Active">Active</SelectItem>
                <SelectItem value="inactive" label="Inactive">Inactive</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Stock */}
          <div className="flex items-center gap-1.5">
            <span className="text-sm text-muted-foreground whitespace-nowrap">Stock</span>
            <Select value={currentStock} onValueChange={handleStockChange}>
              <SelectTrigger className="h-9 w-[130px] text-sm">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {STOCK_OPTIONS.map((o) => <SelectItem key={o.value} value={o.value} label={o.label}>{o.label}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>

          {/* Price range */}
          <div className="flex items-center gap-1.5">
            <span className="text-sm text-muted-foreground whitespace-nowrap">Price</span>
            <Input
              type="text"
              inputMode="numeric"
              placeholder="Min"
              value={minPrice}
              onChange={(e) => setMinPrice(formatNumberInput(e.target.value))}
              onBlur={commitPrice}
              onKeyDown={(e) => e.key === "Enter" && commitPrice()}
              className="h-9 w-[110px] text-sm"
            />
            <span className="text-muted-foreground">—</span>
            <Input
              type="text"
              inputMode="numeric"
              placeholder="Max"
              value={maxPrice}
              onChange={(e) => setMaxPrice(formatNumberInput(e.target.value))}
              onBlur={commitPrice}
              onKeyDown={(e) => e.key === "Enter" && commitPrice()}
              className="h-9 w-[110px] text-sm"
            />
          </div>
        </div>
      )}
    </div>
  );
}
