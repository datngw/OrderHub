"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Package,
  ShoppingCart,
  Users,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { ROUTES } from "@/lib/constants";

const adminNavItems = [
  { label: "Dashboard", href: ROUTES.adminDashboard, icon: LayoutDashboard },
  { label: "Products", href: ROUTES.adminProducts, icon: Package },
  { label: "Orders", href: ROUTES.adminOrders, icon: ShoppingCart },
  //{ label: "Accounts", href: ROUTES.adminAccounts, icon: Users },
];

export function AdminSidebar() {
  const pathname = usePathname();

  return (
    <aside className="hidden w-64 shrink-0 border-r bg-card md:block">
      <div className="flex h-14 items-center border-b px-6">
        <Link href={ROUTES.adminDashboard} className="text-lg font-bold">
          OrderHub
        </Link>
        <span className="ml-2 rounded bg-primary/10 px-1.5 py-0.5 text-xs font-medium text-primary">
          Admin
        </span>
      </div>
      <nav className="space-y-1 p-4">
        {adminNavItems.map((item) => {
          const isActive = pathname.startsWith(item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                isActive
                  ? "bg-accent text-accent-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )}
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
