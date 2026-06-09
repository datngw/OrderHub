"use client";

import Link from "next/link";
import { ShoppingCart, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { ROUTES } from "@/lib/constants";
import { formatPrice } from "@/lib/utils";

interface CartSummaryProps {
  totalItems: number;
  totalAmount: number;
  onClear: () => void;
  isLoading: boolean;
}

export function CartSummary({
  totalItems,
  totalAmount,
  onClear,
  isLoading,
}: CartSummaryProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Order Summary</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex justify-between text-sm">
          <span className="text-muted-foreground">Items</span>
          <span>{totalItems}</span>
        </div>
        <Separator />
        <div className="flex justify-between text-lg font-semibold">
          <span>Total</span>
          <span>{formatPrice(totalAmount)}</span>
        </div>
      </CardContent>
      <CardFooter className="flex-col gap-2">
        <Link href={ROUTES.checkout} className="w-full">
          <Button size="lg" className="w-full cursor-pointer">
            <ShoppingCart className="mr-2 size-4" />
            Proceed to Checkout
          </Button>
        </Link>
        <Button
          variant="outline"
          className="w-full cursor-pointer"
          onClick={onClear}
          disabled={isLoading}
        >
          <Trash2 className="mr-2 size-4" />
          Clear Cart
        </Button>
      </CardFooter>
    </Card>
  );
}
