"use client";

import Link from "next/link";
import { Package } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/shared/empty-state";
import { ROUTES } from "@/lib/constants";
import { useProduct } from "../hooks";
import { ProductDetailContent } from "./product-detail-content";
import { ProductDetailSkeleton } from "./product-detail-skeleton";

interface ProductDetailPageContentProps {
  productId: string;
}

export function ProductDetailPageContent({
  productId,
}: ProductDetailPageContentProps) {
  const { product, isLoading, error } = useProduct(productId);

  if (isLoading) {
    return <ProductDetailSkeleton />;
  }

  if (error || !product) {
    return (
      <EmptyState
        icon={<Package className="size-10" />}
        title="Product not found"
        description={
          error ?? "The product you are looking for does not exist."
        }
        action={
          <Link href={ROUTES.products}>
            <Button variant="outline">Back to Products</Button>
          </Link>
        }
      />
    );
  }

  return <ProductDetailContent product={product} />;
}
