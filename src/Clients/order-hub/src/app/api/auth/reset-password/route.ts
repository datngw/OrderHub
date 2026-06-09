import { NextRequest, NextResponse } from "next/server";
import { apiFetch } from "@/lib/api-client";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    if (!body.email || !body.code || !body.newPassword) {
      return NextResponse.json(
        { message: "Email, code, and new password are required" },
        { status: 400 }
      );
    }

    await apiFetch(API_ENDPOINTS.auth.resetPassword, {
      method: "POST",
      body: JSON.stringify(body),
    });

    return NextResponse.json({ message: "Password reset successfully" });
  } catch (error) {
    return sendErrorResponse(error);
  }
}
