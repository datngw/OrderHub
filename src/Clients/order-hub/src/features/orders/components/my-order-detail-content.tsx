"use client";

import { useState } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  Copy,
  MapPin,
  RefreshCw,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { ROUTES, ORDER_STATUS } from "@/lib/constants";
import { formatPrice } from "@/lib/utils";
import { useOrder } from "../hooks";
import { useAddress } from "@/features/checkout/use-address";
import * as orderApi from "../api";

interface MyOrderDetailContentProps {
  id: string;
}

const STATUS_CHIP: Record<string, { bg: string; text: string }> = {
  [ORDER_STATUS.Pending]: { bg: "bg-yellow-50", text: "text-yellow-700" },
  [ORDER_STATUS.Confirmed]: { bg: "bg-blue-50", text: "text-blue-700" },
  [ORDER_STATUS.Shipped]: { bg: "bg-purple-50", text: "text-purple-700" },
  [ORDER_STATUS.Delivered]: { bg: "bg-green-50", text: "text-green-700" },
  [ORDER_STATUS.Cancelled]: { bg: "bg-red-50", text: "text-red-700" },
};

function fmtPrice(value: number | undefined | null): string {
  if (value == null || isNaN(value)) return "—";
  return formatPrice(value);
}

function fmtDate(dateStr: string | null | undefined): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function MyOrderDetailContent({ id }: MyOrderDetailContentProps) {
  const { order, isLoading, error, refetch } = useOrder(id);
  const { getProvinceName, getDistrictName, getWardName } = useAddress(null, null);
  const [cancelOpen, setCancelOpen] = useState(false);
  const [cancelling, setCancelling] = useState(false);

  async function handleCancel() {
    setCancelling(true);
    try {
      await orderApi.cancelOrder(id);
      toast.success("Order cancelled successfully");
      setCancelOpen(false);
      refetch();
    } catch {
      toast.error("Failed to cancel order. Please try again.");
    } finally {
      setCancelling(false);
    }
  }

  function copyOrderId() {
    navigator.clipboard.writeText(order!.id);
    toast.success("Order ID copied");
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-16">
        <RefreshCw className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-center">
        <p className="mb-4 text-destructive">{error ?? "Order not found"}</p>
        <Link
          href={ROUTES.myOrders}
          className="inline-flex items-center gap-1.5 rounded-lg border border-border bg-background px-2.5 py-1.5 text-sm font-medium hover:bg-muted hover:text-foreground"
        >
          <ArrowLeft className="size-4" /> Back to Orders
        </Link>
      </div>
    );
  }

  const chip = STATUS_CHIP[order.status];
  const canCancel = order.status === ORDER_STATUS.Pending;
  const shortId = order.id.slice(-8).toUpperCase();
  const totalItems = order.items.reduce((sum, item) => sum + item.quantity, 0);
  const wardName = getWardName(Number(order.ward) || null);
  const districtName = getDistrictName(Number(order.district) || null);
  const provinceName = getProvinceName(Number(order.province) || null);
  const fullAddress = [order.streetAddress, wardName, districtName, provinceName]
    .filter(Boolean)
    .join(", ");

  return (
    <div className="space-y-6">
      {/* Back link */}
      <Link
        href={ROUTES.myOrders}
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" /> Back to Orders
      </Link>

      {/* Header */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <h2 className="text-2xl font-bold">Order #{shortId}</h2>
            {chip && (
              <span
                className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium ${chip.bg} ${chip.text}`}
              >
                {order.status}
              </span>
            )}
            <button
              type="button"
              onClick={copyOrderId}
              className="inline-flex items-center gap-1 rounded-md px-1.5 py-0.5 text-xs text-muted-foreground hover:bg-muted hover:text-foreground"
              title="Copy full Order ID"
            >
              <Copy className="size-3" /> Copy ID
            </button>
          </div>
          <p className="mt-1.5 text-sm text-muted-foreground">
            {fmtDate(order.createdAt)}
            {order.updatedAt && (
              <span className="ml-3">
                Updated: {fmtDate(order.updatedAt)}
              </span>
            )}
          </p>
        </div>
        {canCancel && (
          <Button variant="destructive" onClick={() => setCancelOpen(true)} disabled={cancelling}>
            <XCircle className="mr-2 size-4" /> Cancel Order
          </Button>
        )}
      </div>

      {/* Items table */}
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Product</TableHead>
                <TableHead className="text-right">Unit Price</TableHead>
                <TableHead className="text-right">Quantity</TableHead>
                <TableHead className="text-right">Subtotal</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {order.items.map((item) => (
                <TableRow key={item.id ?? item.productId}>
                  <TableCell className="font-medium">
                    <Link href={ROUTES.productDetail(item.productId)} className="hover:underline">
                      {item.productName}
                    </Link>
                  </TableCell>
                  <TableCell className="text-right">{fmtPrice(item.unitPrice)}</TableCell>
                  <TableCell className="text-right">{item.quantity}</TableCell>
                  <TableCell className="text-right font-medium">
                    {fmtPrice(item.subtotal)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {/* Total */}
          <div className="flex justify-end border-t px-6 py-4">
            <div className="w-full max-w-xs">
              <div className="flex justify-between font-semibold text-base">
                <span>Total</span>
                <span className="text-primary">{fmtPrice(order.totalAmount)}</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Shipping & Note — simple inline info */}
      <Card>
        <CardContent className="grid gap-6 py-5 sm:grid-cols-2">
          {/* Shipping Address */}
          <div className="space-y-2">
            <p className="inline-flex items-center gap-1.5 text-sm font-semibold">
              <MapPin className="size-4" /> Shipping Address
            </p>
            <div className="space-y-1 text-sm">
              <p className="font-medium">{order.fullName || "—"}</p>
              {fullAddress && <p className="text-muted-foreground">{fullAddress}</p>}
              {order.phone && (
                <p className="text-muted-foreground">{order.phone}</p>
              )}
            </div>
          </div>

          {/* Note */}
          <div className="space-y-2">
            <p className="text-sm font-semibold">Note</p>
            <p className="text-sm text-muted-foreground">{order.note || "No note"}</p>
          </div>
        </CardContent>
      </Card>

      {/* Cancel Confirm Dialog */}
      <ConfirmDialog
        open={cancelOpen}
        onOpenChange={setCancelOpen}
        title="Cancel Order"
        description="Are you sure you want to cancel this order? This action cannot be undone."
        confirmLabel={cancelling ? "Cancelling..." : "Cancel Order"}
        cancelLabel="Keep Order"
        onConfirm={handleCancel}
      />
    </div>
  );
}
