import type { Metadata } from "next";
import { AdminOrdersPageContent } from "@/features/orders/components/admin-orders-page-content";

export const metadata: Metadata = {
  title: "Orders - Order Hub",
  description: "Manage all customer orders",
};

export default function AdminOrdersPage() {
  return <AdminOrdersPageContent />;
}
