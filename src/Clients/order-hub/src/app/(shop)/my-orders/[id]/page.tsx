import type { Metadata } from "next";
import { MyOrderDetailContent } from "@/features/orders/components/my-order-detail-content";

export const metadata: Metadata = {
  title: "Order Details - Order Hub",
  description: "View order details",
};

export default async function MyOrderDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <MyOrderDetailContent id={id} />
    </div>
  );
}
