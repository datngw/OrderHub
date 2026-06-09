"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Minus, Plus, Loader2, ShoppingCart } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useBasket } from "@/features/basket/hooks";
import { useAuth } from "@/providers/auth-provider";
import { ROUTES } from "@/lib/constants";
import { toast } from "sonner";

interface AddToCartButtonProps {
  productId: string;
  productStock: number;
}

export function AddToCartButton({
  productId,
  productStock,
}: AddToCartButtonProps) {
  const [quantity, setQuantity] = useState(1);
  const [isAdding, setIsAdding] = useState(false);
  const { addItem } = useBasket();
  const { isAuthenticated } = useAuth();
  const router = useRouter();

  const isOutOfStock = productStock === 0;

  async function handleAddToCart() {
    setIsAdding(true);
    try {
      await addItem(productId, quantity);
      toast.success("Added to cart", {
        description: `${quantity} item${quantity > 1 ? "s" : ""} added to your cart`,
      });
    } catch {
      toast.error("Failed to add to cart", {
        description: "Please try again",
      });
    } finally {
      setIsAdding(false);
    }
  }

  function handleLoginRedirect() {
    router.push(ROUTES.login);
  }

  if (isOutOfStock) {
    return (
      <Button size="lg" className="w-full" disabled>
        Out of Stock
      </Button>
    );
  }

  if (!isAuthenticated) {
    return (
      <Button size="lg" className="w-full cursor-pointer" onClick={handleLoginRedirect}>
        Sign in to Add to Cart
      </Button>
    );
  }

  return (
    <div className="space-y-3">
      {productStock > 0 && productStock <= 5 && (
        <p className="text-sm text-muted-foreground">
          Only {productStock} left in stock
        </p>
      )}

      <div className="flex items-center gap-3">
        {/* Quantity selector */}
        <div className="flex items-center rounded-lg border">
          <Button
            variant="ghost"
            size="icon"
            className="h-9 w-9 rounded-r-none"
            onClick={() => setQuantity((q) => Math.max(1, q - 1))}
            disabled={quantity <= 1}
          >
            <Minus className="size-4" />
          </Button>
          <input
            type="number"
            min={1}
            max={productStock}
            value={quantity || ""}
            onChange={(e) => {
              const raw = e.target.value;
              if (raw === "") { setQuantity(0); return; }
              const val = parseInt(raw, 10);
              if (!isNaN(val)) setQuantity(Math.min(Math.max(val, 0), productStock));
            }}
            onBlur={() => { if (!quantity) setQuantity(1); }}
            className="h-9 w-12 border-x bg-transparent text-center text-sm font-medium outline-none [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
          />
          <Button
            variant="ghost"
            size="icon"
            className="h-9 w-9 rounded-l-none"
            onClick={() => setQuantity((q) => Math.min(productStock, q + 1))}
            disabled={quantity >= productStock}
          >
            <Plus className="size-4" />
          </Button>
        </div>

        {/* Add to cart button */}
        <Button
          size="lg"
          className="flex-1 cursor-pointer"
          onClick={handleAddToCart}
          disabled={isAdding}
        >
          {isAdding ? (
            <Loader2 className="mr-2 size-4 animate-spin" />
          ) : (
            <ShoppingCart className="mr-2 size-4" />
          )}
          Add to Cart
        </Button>
      </div>
    </div>
  );
}
