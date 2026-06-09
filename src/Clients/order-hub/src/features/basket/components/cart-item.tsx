"use client";

import { useState } from "react";
import Link from "next/link";
import { Minus, Plus, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { TableCell, TableRow } from "@/components/ui/table";
import { formatPrice } from "@/lib/utils";
import { ROUTES } from "@/lib/constants";
import type { BasketItem } from "../types";

interface CartItemProps {
  item: BasketItem;
  onUpdateQuantity: (productId: string, quantity: number) => void;
  onRemove: (productId: string) => void;
  isUpdating: boolean;
}

export function CartItem({
  item,
  onUpdateQuantity,
  onRemove,
  isUpdating,
}: CartItemProps) {
  const [localQty, setLocalQty] = useState<string | null>(null);
  const displayValue = localQty !== null ? localQty : item.quantity;

  return (
    <TableRow>
      {/* Product info */}
      <TableCell>
        <div>
          <Link
            href={ROUTES.productDetail(item.productId)}
            className="font-medium hover:underline"
          >
            {item.productName}
          </Link>
          <p className="text-xs text-muted-foreground">SKU: {item.sku}</p>
        </div>
      </TableCell>

      {/* Unit price */}
      <TableCell className="text-right">
        {formatPrice(item.unitPrice)}
      </TableCell>

      {/* Quantity */}
      <TableCell>
        <div className="flex items-center justify-center">
          <Button
            variant="outline"
            size="icon"
            className="h-8 w-8 rounded-r-none border-r-0"
            onClick={() => {
              setLocalQty(null);
              onUpdateQuantity(item.productId, item.quantity - 1);
            }}
            disabled={item.quantity <= 1 || isUpdating}
          >
            <Minus className="size-3" />
          </Button>
          <div className="flex h-8 w-12 items-center justify-center border-y border-input bg-transparent text-sm">
            <input
              type="text"
              value={displayValue || ""}
              onChange={(e) => {
                const raw = e.target.value;
                if (raw === "") { setLocalQty(""); return; }
                const val = parseInt(raw, 10);
                if (!isNaN(val) && val >= 0) setLocalQty(String(Math.max(val, 0)));
              }}
              onBlur={() => {
                const val = parseInt(String(displayValue), 10);
                const finalQty = (!displayValue || isNaN(val) || val < 1) ? 1 : val;
                setLocalQty(null);
                onUpdateQuantity(item.productId, finalQty);
              }}
              className="w-full bg-transparent text-center text-sm outline-none"
            />
          </div>
          <Button
            variant="outline"
            size="icon"
            className="h-8 w-8 rounded-l-none border-l-0"
            onClick={() => {
              setLocalQty(null);
              onUpdateQuantity(item.productId, item.quantity + 1);
            }}
            disabled={isUpdating}
          >
            <Plus className="size-3" />
          </Button>
        </div>
      </TableCell>

      {/* Line total */}
      <TableCell className="text-right font-medium">
        {formatPrice(item.lineTotal)}
      </TableCell>

      {/* Remove */}
      <TableCell className="text-right">
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 text-muted-foreground hover:text-destructive"
          onClick={() => onRemove(item.productId)}
          disabled={isUpdating}
        >
          <Trash2 className="size-4" />
        </Button>
      </TableCell>
    </TableRow>
  );
}
