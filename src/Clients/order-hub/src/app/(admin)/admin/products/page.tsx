import type { Metadata } from "next";
import { AdminProductsPageContent } from "@/features/products/components/admin-products-page-content";

export const metadata: Metadata = {
  title: "Products - Order Hub",
  description: "Manage your products",
};

export default function AdminProductsPage() {
  return <AdminProductsPageContent />;
}
