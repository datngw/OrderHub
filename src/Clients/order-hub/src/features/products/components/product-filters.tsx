"use client";

import { useState, useEffect, useRef } from "react";
import { Search } from "lucide-react";
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

interface ProductFiltersProps {
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

const SORT_OPTIONS = [
  { label: "Newest", key: "CreatedAt-desc", sortBy: "CreatedAt", sortOrder: "desc" as const },
  { label: "Price: Low to High", key: "Price-asc", sortBy: "Price", sortOrder: "asc" as const },
  { label: "Price: High to Low", key: "Price-desc", sortBy: "Price", sortOrder: "desc" as const },
  { label: "Name A-Z", key: "Name-asc", sortBy: "Name", sortOrder: "asc" as const },
  { label: "Name Z-A", key: "Name-desc", sortBy: "Name", sortOrder: "desc" as const },
];

export function ProductFilters({
  onFilterChange,
  currentFilters,
}: ProductFiltersProps) {
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

  // Debounced search
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      onFilterChange({
        ...currentFilters,
        search: searchTerm || undefined,
      });
    }, 300);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm]);

  function handlePriceCommit() {
    const min = parseFormattedNumber(minPrice);
    const max = parseFormattedNumber(maxPrice);
    onFilterChange({
      ...currentFilters,
      minPrice: min ? Number(min) : undefined,
      maxPrice: max ? Number(max) : undefined,
    });
  }

  function handlePriceKeyDown(
    e: React.KeyboardEvent<HTMLInputElement>
  ) {
    if (e.key === "Enter") handlePriceCommit();
  }

  function handleCategoryChange(value: string | null) {
    onFilterChange({
      ...currentFilters,
      category: value || undefined,
    });
  }

  function handleSortChange(value: string | null) {
    if (value === null) return;
    const option = SORT_OPTIONS.find((o) => o.key === value);
    if (option) {
      onFilterChange({
        ...currentFilters,
        sortBy: option.sortBy,
        sortOrder: option.sortOrder,
      });
    }
  }

  const currentSortKey = SORT_OPTIONS.find(
    (o) =>
      o.sortBy === currentFilters.sortBy &&
      o.sortOrder === currentFilters.sortOrder
  )?.key ?? "CreatedAt-desc";

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      {/* Search */}
      <div className="relative flex-1 sm:max-w-sm">
        <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder="Search products..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="h-10 pl-8"
        />
      </div>

      {/* Filters row */}
      <div className="flex flex-wrap items-center gap-3">
        {/* Category */}
        <Select
          value={currentFilters.category ?? ""}
          onValueChange={handleCategoryChange}
        >
          <SelectTrigger className="h-10 w-[180px]">
            <SelectValue placeholder="All Categories" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="" label="All Categories">All Categories</SelectItem>
            {CATEGORIES.map((cat) => (
              <SelectItem key={cat} value={cat} label={cat}>
                {cat}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {/* Price range */}
        <div className="flex items-center gap-2">
          <Input
            type="text"
            inputMode="numeric"
            placeholder="Min price"
            value={minPrice}
            onChange={(e) => setMinPrice(formatNumberInput(e.target.value))}
            onBlur={handlePriceCommit}
            onKeyDown={handlePriceKeyDown}
            className="h-10 w-32"
          />
          <span className="text-muted-foreground">—</span>
          <Input
            type="text"
            inputMode="numeric"
            placeholder="Max price"
            value={maxPrice}
            onChange={(e) => setMaxPrice(formatNumberInput(e.target.value))}
            onBlur={handlePriceCommit}
            onKeyDown={handlePriceKeyDown}
            className="h-10 w-32"
          />
        </div>

        {/* Sort */}
        <Select
          value={currentSortKey}
          onValueChange={handleSortChange}
        >
          <SelectTrigger className="h-10 w-[190px]">
            {SORT_OPTIONS.find((o) => o.key === currentSortKey)?.label ?? "Newest"}
          </SelectTrigger>
          <SelectContent>
            {SORT_OPTIONS.map((option) => (
              <SelectItem key={option.key} value={option.key}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    </div>
  );
}
