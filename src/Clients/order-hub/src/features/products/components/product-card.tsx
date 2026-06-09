"use client";

import Link from "next/link";
import { Package } from "lucide-react";
import { Card, CardContent, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ROUTES } from "@/lib/constants";
import { formatPrice } from "@/lib/utils";
import type { Product } from "../types";

interface ProductCardProps {
  product: Product;
}

function getStockStatus(stock: number): {
  label: string;
  variant: "default" | "secondary" | "destructive";
} {
  if (stock === 0) return { label: "Out of Stock", variant: "destructive" };
  if (stock <= 5) return { label: `${stock} left`, variant: "secondary" };
  return { label: `${stock} in stock`, variant: "default" };
}

export function ProductCard({ product }: ProductCardProps) {
  const stockStatus = getStockStatus(product.stock);

  return (
    <Link href={ROUTES.productDetail(product.id)}>
      <Card className="group h-full transition-shadow hover:shadow-md">
        {/* Image area */}
        <div className="aspect-square overflow-hidden rounded-t-xl">
          {product.mainImageUrl ? (
            <img
              src={product.mainImageUrl}
              alt={product.name}
              className="h-full w-full object-cover transition-transform group-hover:scale-105"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center bg-muted">
              <Package className="size-12 text-muted-foreground/50" />
            </div>
          )}
        </div>

        <CardContent className="space-y-2">
          <CardTitle className="line-clamp-1">{product.name}</CardTitle>
          <Badge variant="secondary">{product.category}</Badge>
          <div className="flex items-center justify-between">
            <span className="text-lg font-semibold">
              {formatPrice(product.price)}
            </span>
            <Badge variant={stockStatus.variant} className="text-xs">
              {stockStatus.label}
            </Badge>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
