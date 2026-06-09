"use client";

import { useEffect } from "react";
import { RefreshCw, ShoppingCart } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { EmptyState } from "@/components/shared/empty-state";
import { useBasket } from "../hooks";
import { CartItem } from "./cart-item";
import { CartSummary } from "./cart-summary";
import { toast } from "sonner";
import Link from "next/link";
import { ROUTES } from "@/lib/constants";

export function CartPageContent() {
  const { basket, isLoading, fetchBasket, updateItem, removeItem, clear } =
    useBasket();

  useEffect(() => {
    fetchBasket();
  }, [fetchBasket]);

  async function handleUpdateQuantity(productId: string, quantity: number) {
    try {
      await updateItem(productId, quantity);
    } catch {
      toast.error("Failed to update quantity");
    }
  }

  async function handleRemove(productId: string) {
    try {
      await removeItem(productId);
      toast.success("Item removed from cart");
    } catch {
      toast.error("Failed to remove item");
    }
  }

  async function handleClear() {
    try {
      await clear();
      toast.success("Cart cleared");
    } catch {
      toast.error("Failed to clear cart");
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-16">
        <RefreshCw className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!basket || basket.items.length === 0) {
    return (
      <EmptyState
        icon={<ShoppingCart className="size-10" />}
        title="Your cart is empty"
        description="Add some products to get started"
        action={
          <Link href={ROUTES.products}>
            <Button>Browse Products</Button>
          </Link>
        }
      />
    );
  }

  return (
    <div className="mt-6 grid grid-cols-1 gap-8 lg:grid-cols-3">
      {/* Cart items table */}
      <div className="lg:col-span-2">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Product</TableHead>
              <TableHead className="text-right">Price</TableHead>
              <TableHead className="text-center">Quantity</TableHead>
              <TableHead className="text-right">Total</TableHead>
              <TableHead className="w-12" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {basket.items.map((item) => (
              <CartItem
                key={item.productId}
                item={item}
                onUpdateQuantity={handleUpdateQuantity}
                onRemove={handleRemove}
                isUpdating={isLoading}
              />
            ))}
          </TableBody>
        </Table>
      </div>

      {/* Summary sidebar */}
      <div>
        <CartSummary
          totalItems={basket.totalItems}
          totalAmount={basket.totalAmount}
          onClear={handleClear}
          isLoading={isLoading}
        />
      </div>
    </div>
  );
}
