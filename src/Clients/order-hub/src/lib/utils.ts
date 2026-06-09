import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatPrice(price: number): string {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(price)
}

/**
 * Format a numeric string with thousand separators (Vietnamese locale: dot separator).
 * e.g. "1000000" → "1.000.000"
 */
export function formatNumberInput(value: string): string {
  const digits = value.replace(/\D/g, "")
  if (!digits) return ""
  return Number(digits).toLocaleString("vi-VN")
}

/**
 * Parse a formatted number string back to a plain numeric string.
 * e.g. "1.000.000" → "1000000"
 */
export function parseFormattedNumber(value: string): string {
  return value.replace(/\./g, "").replace(/,/g, "")
}
