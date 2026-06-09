import type { Metadata } from "next";
import { PageHeader } from "@/components/shared/page-header";
import { CartPageContent } from "@/features/basket/components/cart-page-content";

export const metadata: Metadata = {
  title: "Cart - Order Hub",
  description: "Your shopping cart",
};

export default function CartPage() {
  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <PageHeader title="Shopping Cart" />
      <CartPageContent />
    </div>
  );
}
