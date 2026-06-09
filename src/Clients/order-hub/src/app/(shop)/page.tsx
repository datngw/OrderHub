import type { Metadata } from "next";
import Link from "next/link";
import { ROUTES } from "@/lib/constants";

export const metadata: Metadata = {
  title: "Order Hub - Shop",
  description: "Browse products and place orders",
};

export default function ShopHomePage() {
  return (
    <div className="mx-auto max-w-7xl px-4 py-12">
      <div className="text-center">
        <h1 className="text-4xl font-bold tracking-tight">Welcome to Order Hub</h1>
        <p className="mt-4 text-lg text-muted-foreground">
          Browse our products and place orders with ease
        </p>
        <Link
          href={ROUTES.products}
          className="mt-8 inline-flex h-10 items-center justify-center rounded-md bg-primary px-6 text-sm font-medium text-primary-foreground hover:bg-primary/90"
        >
          Browse Products
        </Link>
      </div>
    </div>
  );
}
