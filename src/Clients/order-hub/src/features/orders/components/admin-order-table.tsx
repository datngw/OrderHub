"use client";

import {
  Trash2,
  XCircle,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ORDER_STATUS } from "@/lib/constants";
import { formatPrice } from "@/lib/utils";
import type { Order } from "../types";

interface AdminOrderTableProps {
  orders: Order[];
  isLoading: boolean;
  onViewDetail: (order: Order) => void;
  onQuickStatusUpdate: (order: Order, newStatus: string) => void;
  onCancelOrder: (order: Order) => void;
  onDeleteOrder: (order: Order) => void;
}

const STATUS_CHIP: Record<string, { bg: string; text: string }> = {
  [ORDER_STATUS.Pending]: { bg: "bg-yellow-50", text: "text-yellow-700" },
  [ORDER_STATUS.Confirmed]: { bg: "bg-blue-50", text: "text-blue-700" },
  [ORDER_STATUS.Shipped]: { bg: "bg-purple-50", text: "text-purple-700" },
  [ORDER_STATUS.Delivered]: { bg: "bg-green-50", text: "text-green-700" },
  [ORDER_STATUS.Cancelled]: { bg: "bg-red-50", text: "text-red-700" },
};

const NEXT_STATUS: Record<string, { label: string; status: string }> = {
  [ORDER_STATUS.Pending]: { label: "Confirm", status: ORDER_STATUS.Confirmed },
  [ORDER_STATUS.Confirmed]: { label: "Ship", status: ORDER_STATUS.Shipped },
  [ORDER_STATUS.Shipped]: { label: "Deliver", status: ORDER_STATUS.Delivered },
};

function TableSkeleton() {
  return (
    <Card>
      <CardContent className="p-0">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Order #</TableHead>
              <TableHead>Customer</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Items</TableHead>
              <TableHead className="text-right">Total</TableHead>
              <TableHead>Date</TableHead>
              <TableHead>Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {Array.from({ length: 6 }).map((_, i) => (
              <TableRow key={i}>
                <TableCell>
                  <Skeleton className="h-4 w-20" />
                </TableCell>
                <TableCell>
                  <Skeleton className="h-4 w-32" />
                  <Skeleton className="mt-1 h-3 w-24" />
                </TableCell>
                <TableCell>
                  <Skeleton className="h-5 w-20" />
                </TableCell>
                <TableCell>
                  <Skeleton className="ml-auto h-4 w-8" />
                </TableCell>
                <TableCell>
                  <Skeleton className="ml-auto h-4 w-24" />
                </TableCell>
                <TableCell>
                  <Skeleton className="h-4 w-28" />
                </TableCell>
                <TableCell>
                  <div className="flex gap-1">
                    <Skeleton className="h-7 w-16" />
                    <Skeleton className="h-7 w-14" />
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}

function fmtDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

export function AdminOrderTable({
  orders,
  isLoading,
  onViewDetail,
  onQuickStatusUpdate,
  onCancelOrder,
  onDeleteOrder,
}: AdminOrderTableProps) {
  if (isLoading) {
    return <TableSkeleton />;
  }

  if (orders.length === 0) {
    return null;
  }

  return (
    <Card>
      <CardContent className="p-0">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Order #</TableHead>
              <TableHead>Customer</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Items</TableHead>
              <TableHead className="text-right">Total</TableHead>
              <TableHead>Date</TableHead>
              <TableHead>Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {orders.map((order) => {
              const chip = STATUS_CHIP[order.status];
              const shortId = order.id.slice(-8).toUpperCase();
              const itemCount = order.items.reduce(
                (sum, item) => sum + item.quantity,
                0
              );
              const isDelivered = order.status === ORDER_STATUS.Delivered;
              const canCancel =
                order.status === ORDER_STATUS.Pending ||
                order.status === ORDER_STATUS.Confirmed;
              const canDelete = !isDelivered;
              const next = NEXT_STATUS[order.status];

              return (
                <TableRow
                  key={order.id}
                  className="cursor-pointer"
                  onClick={() => onViewDetail(order)}
                >
                  {/* Order # */}
                  <TableCell className="font-medium">
                    <span className="hover:underline">#{shortId}</span>
                  </TableCell>

                  {/* Customer */}
                  <TableCell>
                    <div className="font-medium">
                      {order.fullName || "—"}
                    </div>
                    <div className="text-xs text-muted-foreground">
                      {order.email}
                    </div>
                  </TableCell>

                  {/* Status */}
                  <TableCell>
                    {chip ? (
                      <span
                        className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${chip.bg} ${chip.text}`}
                      >
                        {order.status}
                      </span>
                    ) : (
                      <Badge variant="secondary">{order.status}</Badge>
                    )}
                  </TableCell>

                  {/* Items */}
                  <TableCell className="text-right">{itemCount}</TableCell>

                  {/* Total */}
                  <TableCell className="text-right font-medium">
                    {formatPrice(order.totalAmount)}
                  </TableCell>

                  {/* Date */}
                  <TableCell className="text-muted-foreground">
                    {fmtDate(order.createdAt)}
                  </TableCell>

                  {/* Actions */}
                  <TableCell>
                    <div
                      className="flex items-center gap-1"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {next && (
                        <Button
                          variant="outline"
                          size="sm"
                          className="h-7 cursor-pointer text-xs"
                          onClick={() =>
                            onQuickStatusUpdate(order, next.status)
                          }
                        >
                          {next.label}
                        </Button>
                      )}
                      {canCancel && (
                        <Button
                          variant="outline"
                          size="sm"
                          className="h-7 cursor-pointer text-xs text-destructive hover:bg-destructive hover:text-white"
                          onClick={() => onCancelOrder(order)}
                        >
                          <XCircle className="mr-1 size-3" />
                          Cancel
                        </Button>
                      )}
                      {canDelete && (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-7 cursor-pointer text-xs text-muted-foreground hover:text-destructive"
                          onClick={() => onDeleteOrder(order)}
                        >
                          <Trash2 className="size-3" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}
