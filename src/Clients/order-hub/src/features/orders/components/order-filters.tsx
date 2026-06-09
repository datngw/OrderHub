"use client";

import { useState, useEffect, useRef } from "react";
import { Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { OrderListParams } from "../types";

interface OrderFiltersProps {
  onFilterChange: (partial: Partial<OrderListParams>) => void;
}

const SORT_OPTIONS = [
  { label: "Newest", key: "newest", sortBy: "CreatedAt", sortOrder: "desc" as const },
  { label: "Oldest", key: "oldest", sortBy: "CreatedAt", sortOrder: "asc" as const },
];

export function OrderFilters({ onFilterChange }: OrderFiltersProps) {
  const [searchTerm, setSearchTerm] = useState("");
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(null);
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [sortKey, setSortKey] = useState("newest");

  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      onFilterChange({
        search: searchTerm || undefined,
        page: 1,
      });
    }, 300);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm]);

  function handleFromDateChange(value: string) {
    setFromDate(value);
    onFilterChange({
      fromDate: value || undefined,
      page: 1,
    });
  }

  function handleToDateChange(value: string) {
    setToDate(value);
    onFilterChange({
      toDate: value || undefined,
      page: 1,
    });
  }

  function handleSortChange(value: string | null) {
    if (!value) return;
    setSortKey(value);
    const option = SORT_OPTIONS.find((o) => o.key === value);
    if (option) {
      onFilterChange({
        sortBy: option.sortBy,
        sortOrder: option.sortOrder,
        page: 1,
      });
    }
  }

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      {/* Search */}
      <div className="relative flex-1 sm:max-w-sm">
        <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder="Search orders..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="h-10 pl-8"
        />
      </div>

      {/* Filters row */}
      <div className="flex flex-wrap items-center gap-3">
        {/* Date range */}
        <Input
          type="date"
          value={fromDate}
          onChange={(e) => handleFromDateChange(e.target.value)}
          className="h-10 w-[150px]"
          placeholder="From"
        />
        <span className="text-muted-foreground">—</span>
        <Input
          type="date"
          value={toDate}
          onChange={(e) => handleToDateChange(e.target.value)}
          className="h-10 w-[150px]"
          placeholder="To"
        />

        {/* Sort */}
        <Select value={sortKey} onValueChange={handleSortChange}>
          <SelectTrigger className="h-10 w-[150px]">
            {SORT_OPTIONS.find((o) => o.key === sortKey)?.label ?? "Newest"}
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
