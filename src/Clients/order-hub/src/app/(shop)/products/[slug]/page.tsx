import type { Metadata } from "next";
import { ProductDetailPageContent } from "@/features/products/components/product-detail-page-content";

export const metadata: Metadata = {
  title: "Product Detail - Order Hub",
  description: "View product details",
};

export default async function ProductDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;

  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <ProductDetailPageContent productId={slug} />
    </div>
  );
}
