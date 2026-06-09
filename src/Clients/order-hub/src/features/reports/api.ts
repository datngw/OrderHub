import { clientFetch } from "@/lib/client-fetch";
import type { TopProductRevenue, RevenueByDay, ReportParams } from "./types";

export async function getTopProducts(params?: ReportParams): Promise<TopProductRevenue[]> {
  const query = new URLSearchParams();
  if (params?.from) query.set("from", params.from);
  if (params?.to) query.set("to", params.to);
  if (params?.top) query.set("top", String(params.top));

  return clientFetch<TopProductRevenue[]>(`/api/admin/reports/top-products?${query}`);
}

export async function getRevenueByDay(params?: ReportParams): Promise<RevenueByDay[]> {
  const query = new URLSearchParams();
  if (params?.from) query.set("from", params.from);
  if (params?.to) query.set("to", params.to);

  return clientFetch<RevenueByDay[]>(`/api/admin/reports/revenue-by-day?${query}`);
}
