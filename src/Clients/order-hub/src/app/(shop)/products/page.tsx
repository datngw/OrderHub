import type { Metadata } from "next";
import { PageHeader } from "@/components/shared/page-header";
import { ProductListPageContent } from "@/features/products/components/product-list-page-content";

export const metadata: Metadata = {
  title: "Products - Order Hub",
  description: "Browse our products",
};

export default function ShopProductsPage() {
  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <PageHeader title="Products" description="Browse our catalog" />
      <ProductListPageContent />
    </div>
  );
}
