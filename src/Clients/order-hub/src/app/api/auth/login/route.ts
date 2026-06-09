import { NextRequest, NextResponse } from "next/server";
import { apiFetch } from "@/lib/api-client";
import { setAuthCookies } from "@/lib/auth";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    if (!body.email || !body.password) {
      return NextResponse.json(
        { message: "Email and password are required" },
        { status: 400 }
      );
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
    }>(API_ENDPOINTS.auth.login, {
      method: "POST",
      body: JSON.stringify({ email: body.email, password: body.password }),
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
    return sendErrorResponse(error);
  }
}
