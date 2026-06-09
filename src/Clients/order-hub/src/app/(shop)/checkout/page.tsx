import type { Metadata } from "next";
import { PageHeader } from "@/components/shared/page-header";
import { CheckoutForm } from "@/features/checkout/components/checkout-form";

export const metadata: Metadata = {
  title: "Checkout - Order Hub",
  description: "Complete your order",
};

export default function CheckoutPage() {
  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <PageHeader title="Checkout" description="Complete your order" />
      <div className="mt-6">
        <CheckoutForm />
      </div>
    </div>
  );
}
