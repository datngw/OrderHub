"use client";

import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { ROUTES } from "@/lib/constants";
import { formatPrice } from "@/lib/utils";
import type { Product } from "../types";
import { ProductGallery } from "./product-gallery";
import { AddToCartButton } from "./add-to-cart-button";

interface ProductDetailContentProps {
  product: Product;
}

function getStockLabel(stock: number): {
  label: string;
  variant: "default" | "secondary" | "destructive";
} {
  if (stock === 0) return { label: "Out of Stock", variant: "destructive" };
  if (stock <= 5) return { label: `Only ${stock} left`, variant: "secondary" };
  return { label: `${stock} in stock`, variant: "default" };
}

export function ProductDetailContent({ product }: ProductDetailContentProps) {
  const stockStatus = getStockLabel(product.stock);

  return (
    <div className="grid grid-cols-1 gap-8 lg:grid-cols-2">
      {/* Left: Gallery */}
      <ProductGallery
        mainImageUrl={product.mainImageUrl}
        galleryImageUrls={product.galleryImageUrls}
      />

      {/* Right: Product info */}
      <div className="flex flex-col gap-4">
        {/* Back link */}
        <Link
          href={ROUTES.products}
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" />
          Back to Products
        </Link>

        {/* Category */}
        <Badge variant="secondary" className="w-fit">
          {product.category}
        </Badge>

        {/* Name */}
        <h1 className="text-3xl font-bold tracking-tight">
          {product.name}
        </h1>

        {/* SKU */}
        <p className="text-sm text-muted-foreground">SKU: {product.sku}</p>

        {/* Price */}
        <div className="text-2xl font-semibold">
          {formatPrice(product.price)}
        </div>

        {/* Stock status */}
        <Badge variant={stockStatus.variant} className="w-fit">
          {stockStatus.label}
        </Badge>

        <Separator />

        {/* Description */}
        <div>
          <h2 className="mb-2 text-lg font-semibold">Description</h2>
          <p className="whitespace-pre-line text-muted-foreground">
            {product.description}
          </p>
        </div>

        <Separator />

        {/* Add to cart */}
        <AddToCartButton
          productId={product.id}
          productStock={product.stock}
        />
      </div>
    </div>
  );
}
