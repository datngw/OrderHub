import { NextRequest, NextResponse } from "next/server";
import { apiFetchWithRefresh } from "@/lib/api-with-refresh";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function PUT(
  request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    const body = await request.json();

    await apiFetchWithRefresh(API_ENDPOINTS.orders.updateStatus(id), {
      method: "PUT",
      body: JSON.stringify(body),
    });

    return new NextResponse(null, { status: 204 });
  } catch (error) {
    return sendErrorResponse(error);
  }
}
