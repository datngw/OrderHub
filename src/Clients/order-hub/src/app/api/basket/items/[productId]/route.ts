import { NextRequest, NextResponse } from "next/server";
import { apiFetchWithRefresh } from "@/lib/api-with-refresh";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

export async function PUT(
  request: NextRequest,
  { params }: { params: Promise<{ productId: string }> }
) {
  try {
    const { productId } = await params;
    const body = await request.json();
    const data = await apiFetchWithRefresh(API_ENDPOINTS.basket.updateItem(productId), {
      method: "PUT",
      body: JSON.stringify(body),
    });

    return NextResponse.json(data);
  } catch (error) {
    return sendErrorResponse(error);
  }
}

export async function DELETE(
  _request: NextRequest,
  { params }: { params: Promise<{ productId: string }> }
) {
  try {
    const { productId } = await params;
    const data = await apiFetchWithRefresh(API_ENDPOINTS.basket.removeItem(productId), {
      method: "DELETE",
    });

    return NextResponse.json(data);
  } catch (error) {
    return sendErrorResponse(error);
  }
}
