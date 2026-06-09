"use client";

import { UserNav } from "@/components/shared/user-nav";

export function AdminHeader() {
  return (
    <header className="flex h-14 items-center justify-end border-b px-6">
      <UserNav />
    </header>
  );
}
