import { NextRequest, NextResponse } from "next/server";
import { apiFetch } from "@/lib/api-client";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    if (!body.email) {
      return NextResponse.json({ message: "Email is required" }, { status: 400 });
    }

    await apiFetch(API_ENDPOINTS.auth.forgotPassword, {
      method: "POST",
      body: JSON.stringify({ email: body.email }),
    });

    return NextResponse.json({ message: "Reset code sent to your email" });
  } catch (error) {
    return sendErrorResponse(error);
  }
}
