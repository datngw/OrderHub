import { clientFetch } from "@/lib/client-fetch";

export async function changePasswordApi(currentPassword: string, newPassword: string): Promise<void> {
  return clientFetch<void>("/api/auth/change-password", {
    method: "POST",
    body: JSON.stringify({ currentPassword, newPassword }),
  });
}
