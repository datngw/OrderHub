import type { Metadata } from "next";
import { AdminOrderDetailContent } from "@/features/orders/components/admin-order-detail-content";

export const metadata: Metadata = {
  title: "Order Detail - Order Hub",
  description: "View and manage order",
};

export default async function AdminOrderDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  return <AdminOrderDetailContent id={id} />;
}
