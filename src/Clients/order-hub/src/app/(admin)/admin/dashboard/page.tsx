"use client";

import { useEffect, useState } from "react";
import { format } from "date-fns";
import type { DateRange } from "react-day-picker";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  XAxis,
  YAxis,
} from "recharts";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/shared/page-header";
import { StatCard } from "@/components/shared/stat-card";
import { DateRangePicker } from "@/components/shared/date-range-picker";
import { ShoppingCart, DollarSign, TrendingUp } from "lucide-react";
import { getTopProducts, getRevenueByDay } from "@/features/reports/api";
import type { TopProductRevenue, RevenueByDay } from "@/features/reports/types";
import { formatPrice } from "@/lib/utils";

const CHART_COLORS = [
  "hsl(var(--chart-1))",
  "hsl(var(--chart-2))",
  "hsl(var(--chart-3))",
  "hsl(var(--chart-4))",
  "hsl(var(--chart-5))",
];

export default function AdminDashboardPage() {
  const [revenueData, setRevenueData] = useState<RevenueByDay[]>([]);
  const [topProducts, setTopProducts] = useState<TopProductRevenue[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dateRange, setDateRange] = useState<DateRange | undefined>(() => {
    const to = new Date();
    const from = new Date();
    from.setDate(from.getDate() - 30);
    return { from, to };
  });

  useEffect(() => {
    async function fetchData() {
      setLoading(true);
      setError(null);
      try {
        const params = {
          from: dateRange?.from ? format(dateRange.from, "yyyy-MM-dd") : undefined,
          to: dateRange?.to ? format(dateRange.to, "yyyy-MM-dd") : undefined,
        };
        const [revenue, products] = await Promise.all([
          getRevenueByDay(params),
          getTopProducts({ ...params, top: 5 }),
        ]);
        setRevenueData(revenue);
        setTopProducts(products);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load data");
      } finally {
        setLoading(false);
      }
    }
    fetchData();
  }, [dateRange]);

  const totalOrders = revenueData.reduce((sum, d) => sum + d.orderCount, 0);
  const totalRevenue = revenueData.reduce(
    (sum, d) => sum + d.totalRevenue,
    0
  );

  const chartRevenueData = revenueData.map((d) => ({
    ...d,
    label: new Date(d.date).toLocaleDateString("vi-VN", {
      day: "2-digit",
      month: "2-digit",
    }),
  }));

  const chartProductData = topProducts.map((p) => ({
    ...p,
    shortName:
      p.productName.length > 20
        ? p.productName.slice(0, 20) + "…"
        : p.productName,
  }));

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader title="Dashboard" description="Overview & Reports" />
        <DateRangePicker date={dateRange} onDateChange={setDateRange} />
      </div>

      {/* Stat Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        {loading ? (
          <>
            <Skeleton className="h-[120px] rounded-xl" />
            <Skeleton className="h-[120px] rounded-xl" />
            <Skeleton className="h-[120px] rounded-xl" />
          </>
        ) : (
          <>
            <StatCard
              title="Total Orders"
              value={totalOrders.toLocaleString("vi-VN")}
              icon={<ShoppingCart className="h-4 w-4 text-muted-foreground" />}
            />
            <StatCard
              title="Total Revenue"
              value={formatPrice(totalRevenue)}
              icon={<DollarSign className="h-4 w-4 text-muted-foreground" />}
            />
            <StatCard
              title="Best Seller"
              value={
                topProducts.length > 0 ? topProducts[0].productName : "—"
              }
              description={
                topProducts.length > 0
                  ? `${topProducts[0].totalQuantity} sold`
                  : undefined
              }
              icon={<TrendingUp className="h-4 w-4 text-muted-foreground" />}
            />
          </>
        )}
      </div>

      {/* Error */}
      {error && (
        <Card>
          <CardContent className="py-8 text-center text-destructive">
            {error}
          </CardContent>
        </Card>
      )}

      {/* Charts */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Revenue by Day */}
        <Card>
          <CardHeader>
            <CardTitle>Revenue by Day</CardTitle>
            <CardDescription>Daily order revenue overview</CardDescription>
          </CardHeader>
          <CardContent>
            {loading ? (
              <Skeleton className="h-[300px] w-full" />
            ) : chartRevenueData.length === 0 ? (
              <p className="py-12 text-center text-sm text-muted-foreground">
                No revenue data available
              </p>
            ) : (
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartRevenueData}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} />
                  <XAxis
                    dataKey="label"
                    tick={{ fontSize: 12 }}
                    tickLine={false}
                    axisLine={false}
                  />
                  <YAxis
                    tick={{ fontSize: 12 }}
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(v: number) =>
                      v >= 1_000_000
                        ? `${(v / 1_000_000).toFixed(1)}M`
                        : v >= 1_000
                          ? `${(v / 1_000).toFixed(0)}K`
                          : String(v)
                    }
                  />
                  <Bar
                    dataKey="totalRevenue"
                    radius={[4, 4, 0, 0]}
                    fill={CHART_COLORS[0]}
                  />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        {/* Top Products */}
        <Card>
          <CardHeader>
            <CardTitle>Top Products</CardTitle>
            <CardDescription>Best selling products by revenue</CardDescription>
          </CardHeader>
          <CardContent>
            {loading ? (
              <Skeleton className="h-[300px] w-full" />
            ) : chartProductData.length === 0 ? (
              <p className="py-12 text-center text-sm text-muted-foreground">
                No product data available
              </p>
            ) : (
              <ResponsiveContainer width="100%" height={300}>
                <BarChart
                  data={chartProductData}
                  layout="vertical"
                  margin={{ left: 20 }}
                >
                  <CartesianGrid
                    strokeDasharray="3 3"
                    horizontal={false}
                  />
                  <XAxis
                    type="number"
                    tick={{ fontSize: 12 }}
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(v: number) =>
                      v >= 1_000_000
                        ? `${(v / 1_000_000).toFixed(1)}M`
                        : v >= 1_000
                          ? `${(v / 1_000).toFixed(0)}K`
                          : String(v)
                    }
                  />
                  <YAxis
                    type="category"
                    dataKey="shortName"
                    tick={{ fontSize: 12 }}
                    tickLine={false}
                    axisLine={false}
                    width={120}
                  />
                  <Bar dataKey="totalRevenue" radius={[0, 4, 4, 0]}>
                    {chartProductData.map((_, index) => (
                      <Cell
                        key={`cell-${index}`}
                        fill={CHART_COLORS[index % CHART_COLORS.length]}
                      />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
