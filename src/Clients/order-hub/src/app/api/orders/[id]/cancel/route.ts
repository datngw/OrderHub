import { NextRequest, NextResponse } from "next/server";
import { apiFetchWithRefresh } from "@/lib/api-with-refresh";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function POST(
  _request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    await apiFetchWithRefresh(API_ENDPOINTS.orders.cancel(id), { method: "POST" });

    return new NextResponse(null, { status: 204 });
  } catch (error) {
    return sendErrorResponse(error);
  }
}
