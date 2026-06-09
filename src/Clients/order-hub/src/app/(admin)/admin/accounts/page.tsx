import type { Metadata } from "next";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";

export const metadata: Metadata = {
  title: "Accounts - Order Hub",
  description: "Manage user accounts",
};

export default function AdminAccountsPage() {
  return (
    <div className="space-y-6">
      <PageHeader title="Accounts" description="Manage user accounts" />
      <EmptyState
        title="No accounts yet"
        description="Registered users will appear here"
      />
    </div>
  );
}
