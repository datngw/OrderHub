import { NextRequest, NextResponse } from "next/server";
import { apiFetchWithRefresh } from "@/lib/api-with-refresh";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const qs = searchParams.toString();
    const data = await apiFetchWithRefresh(`${API_ENDPOINTS.orders.myOrders}${qs ? `?${qs}` : ""}`);

    return NextResponse.json(data);
  } catch (error) {
    return sendErrorResponse(error);
  }
}
