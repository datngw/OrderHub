import { NextResponse } from "next/server";
import { apiFetch } from "@/lib/api-client";
import { getRefreshToken, clearAuthCookies, getAccessToken } from "@/lib/auth";
import { API_ENDPOINTS } from "@/lib/api-endpoints";

export async function POST() {
  try {
    const refreshToken = await getRefreshToken();
    const accessToken = await getAccessToken();

    if (refreshToken && accessToken) {
      await apiFetch(API_ENDPOINTS.auth.logout, {
        method: "POST",
        body: JSON.stringify({ refreshToken }),
      }, accessToken).catch(() => {});
    }

    await clearAuthCookies();
    return NextResponse.json({ message: "Logged out successfully" });
  } catch {
    await clearAuthCookies();
    return NextResponse.json({ message: "Logged out successfully" });
  }
}
