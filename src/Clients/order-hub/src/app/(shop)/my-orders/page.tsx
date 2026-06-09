import type { Metadata } from "next";
import { PageHeader } from "@/components/shared/page-header";
import { MyOrdersPageContent } from "@/features/orders/components/my-orders-page-content";

export const metadata: Metadata = {
  title: "My Orders - Order Hub",
  description: "View your order history",
};

export default function MyOrdersPage() {
  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <PageHeader title="My Orders" description="View and track your orders" />
      <MyOrdersPageContent />
    </div>
  );
}
