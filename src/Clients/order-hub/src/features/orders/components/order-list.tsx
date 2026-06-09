"use client";

import { ShoppingBag } from "lucide-react";
import { EmptyState } from "@/components/shared/empty-state";
import type { Order } from "../types";
import { OrderCard } from "./order-card";

interface OrderListProps {
  orders: Order[];
}

export function OrderList({ orders }: OrderListProps) {
  if (orders.length === 0) {
    return (
      <EmptyState
        icon={<ShoppingBag className="size-10" />}
        title="No orders yet"
        description="Your orders will appear here once you place them"
      />
    );
  }

  return (
    <div className="flex flex-col gap-3">
      {orders.map((order) => (
        <OrderCard key={order.id} order={order} />
      ))}
    </div>
  );
}
