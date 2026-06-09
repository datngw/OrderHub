"use client";

import Link from "next/link";
import { Calendar, ChevronRight, ShoppingBag } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { ROUTES, ORDER_STATUS } from "@/lib/constants";
import { formatPrice } from "@/lib/utils";
import type { Order } from "../types";

interface OrderCardProps {
  order: Order;
}

const STATUS_CHIP: Record<string, { bg: string; text: string }> = {
  [ORDER_STATUS.Pending]:   { bg: "bg-yellow-50",  text: "text-yellow-700" },
  [ORDER_STATUS.Confirmed]: { bg: "bg-blue-50",    text: "text-blue-700" },
  [ORDER_STATUS.Shipped]:   { bg: "bg-purple-50",  text: "text-purple-700" },
  [ORDER_STATUS.Delivered]: { bg: "bg-green-50",   text: "text-green-700" },
  [ORDER_STATUS.Cancelled]: { bg: "bg-red-50",     text: "text-red-700" },
};

export function OrderCard({ order }: OrderCardProps) {
  const chip = STATUS_CHIP[order.status];
  const shortId = order.id.slice(-8).toUpperCase();
  const itemCount = order.items.reduce((sum, item) => sum + item.quantity, 0);
  const formattedDate = new Date(order.createdAt).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });

  return (
    <Link href={ROUTES.myOrderDetail(order.id)}>
      <Card className="group transition-shadow hover:shadow-md">
        <CardContent className="flex items-center gap-4 p-4 sm:p-6">
          {/* Icon */}
          <div className="flex size-12 shrink-0 items-center justify-center rounded-lg bg-muted">
            <ShoppingBag className="size-6 text-muted-foreground" />
          </div>

          {/* Info */}
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-semibold">#{shortId}</span>
              {chip && (
                <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${chip.bg} ${chip.text}`}>
                  {order.status}
                </span>
              )}
            </div>
            <div className="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
              <span className="inline-flex items-center gap-1">
                <Calendar className="size-3.5" />
                {formattedDate}
              </span>
              <span>{itemCount} {itemCount === 1 ? "item" : "items"}</span>
            </div>
          </div>

          {/* Amount + chevron */}
          <div className="flex shrink-0 items-center gap-2">
            <span className="text-lg font-semibold">{formatPrice(order.totalAmount)}</span>
            <ChevronRight className="size-5 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
