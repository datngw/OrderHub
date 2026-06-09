import { NextRequest, NextResponse } from "next/server";
import { apiFetchWithRefresh } from "@/lib/api-with-refresh";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    if (!body.currentPassword || !body.newPassword) {
      return NextResponse.json(
        { message: "Current password and new password are required" },
        { status: 400 }
      );
    }

    await apiFetchWithRefresh(API_ENDPOINTS.auth.changePassword, {
      method: "POST",
      body: JSON.stringify(body),
    });

    return NextResponse.json({ message: "Password changed successfully" });
  } catch (error) {
    return sendErrorResponse(error);
  }
}
