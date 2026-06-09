"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { ShoppingCart, User } from "lucide-react";
import { Button } from "@/components/ui/button";
import { UserNav } from "@/components/shared/user-nav";
import { useAuth } from "@/providers/auth-provider";
import { useBasket } from "@/features/basket/hooks";
import { ROUTES } from "@/lib/constants";

export function ShopHeader() {
  const pathname = usePathname();
  const { isAuthenticated } = useAuth();
  const { basket } = useBasket();
  const cartCount = basket?.totalItems ?? 0;

  return (
    <header className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-4">
        <div className="flex items-center gap-6">
          <Link href={ROUTES.home} className="text-lg font-bold">
            OrderHub
          </Link>
          <nav className="hidden items-center gap-4 md:flex">
            <Link
              href={ROUTES.products}
              className={
                pathname === ROUTES.products
                  ? "text-foreground font-medium"
                  : "text-muted-foreground hover:text-foreground"
              }
            >
              Products
            </Link>
          </nav>
        </div>
        <div className="flex items-center gap-2">
          {isAuthenticated ? (
            <>
              <Link href={ROUTES.cart} className="relative">
                <Button variant="ghost" size="icon">
                  <ShoppingCart className="h-5 w-5" />
                  {cartCount > 0 && (
                    <span className="absolute -right-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold text-primary-foreground">
                      {cartCount > 99 ? "99+" : cartCount}
                    </span>
                  )}
                </Button>
              </Link>
              <UserNav />
            </>
          ) : (
            <Link href={ROUTES.login}>
              <Button variant="ghost" size="sm">
                <User className="mr-1 h-4 w-4" />
                Sign In
              </Button>
            </Link>
          )}
        </div>
      </div>
    </header>
  );
}
