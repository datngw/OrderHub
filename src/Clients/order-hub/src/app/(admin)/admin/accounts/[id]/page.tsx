import type { Metadata } from "next";
import { PageHeader } from "@/components/shared/page-header";

export const metadata: Metadata = {
  title: "Account Detail - Order Hub",
  description: "View user account details",
};

export default function AdminAccountDetailPage() {
  return (
    <div className="space-y-6">
      <PageHeader title="Account Detail" description="View user information" />
      <div className="rounded-lg border bg-card p-6 text-center text-muted-foreground">
        Account detail coming soon
      </div>
    </div>
  );
}
