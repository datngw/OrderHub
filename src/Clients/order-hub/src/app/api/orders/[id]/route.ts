import { NextRequest, NextResponse } from "next/server";
import { apiFetchWithRefresh } from "@/lib/api-with-refresh";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function GET(
  _request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    const data = await apiFetchWithRefresh(API_ENDPOINTS.orders.detail(id));
    return NextResponse.json(data);
  } catch (error) {
    return sendErrorResponse(error);
  }
}

export async function DELETE(
  _request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    await apiFetchWithRefresh(API_ENDPOINTS.orders.delete(id), { method: "DELETE" });
    return new NextResponse(null, { status: 204 });
  } catch (error) {
    return sendErrorResponse(error);
  }
}
