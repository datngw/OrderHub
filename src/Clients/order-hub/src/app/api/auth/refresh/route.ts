import { NextRequest, NextResponse } from "next/server";
import { apiFetch } from "@/lib/api-client";
import { setAuthCookies, getRefreshToken, clearAuthCookies } from "@/lib/auth";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";
import { ApiError } from "@/types/common";

export async function POST(request: NextRequest) {
  try {
    let body: { refreshToken?: string } = {};
    try {
      if (request.headers.get("content-type")?.includes("application/json")) {
        body = await request.json();
      }
    } catch {
      // Safe fallback for empty or non-JSON body
    }
    const refreshToken = body.refreshToken || (await getRefreshToken());

    if (!refreshToken) {
      return NextResponse.json({ message: "No refresh token provided" }, { status: 401 });
    }

    const loginResponse = await apiFetch<{
      accessToken: string;
      refreshToken: string;
      email: string;
      fullName: string;
      phone: string;
      role: string;
      accessTokenExpiresAt: string;
      refreshTokenExpiresAt: string;
    }>(API_ENDPOINTS.auth.refresh, {
      method: "POST",
      body: JSON.stringify({ refreshToken }),
    });

    await setAuthCookies(loginResponse);

    return NextResponse.json({
      user: {
        email: loginResponse.email,
        fullName: loginResponse.fullName,
        phone: loginResponse.phone,
        role: loginResponse.role,
      },
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      await clearAuthCookies();
    }
    return sendErrorResponse(error);
  }
}
