import { NextResponse } from "next/server";
import { apiFetchWithRefresh } from "@/lib/api-with-refresh";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function GET() {
  try {
    const data = await apiFetchWithRefresh(API_ENDPOINTS.basket.get);
    return NextResponse.json(data);
  } catch (error) {
    return sendErrorResponse(error);
  }
}

export async function DELETE() {
  try {
    await apiFetchWithRefresh(API_ENDPOINTS.basket.clear, { method: "DELETE" });
    return new NextResponse(null, { status: 204 });
  } catch (error) {
    return sendErrorResponse(error);
  }
}
