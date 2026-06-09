import { NextRequest, NextResponse } from "next/server";
import { apiFetch } from "@/lib/api-client";
import { apiFetchWithRefresh } from "@/lib/api-with-refresh";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

/** Mock product image — replace with real images in production */
const MOCK_PRODUCT_IMAGE = "/products/sennheiser-headphone.jpg";

export async function GET(
  _request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    const data = await apiFetch(API_ENDPOINTS.products.detail(id));

    // Inject mock image if product has none
    if (data && typeof data === "object") {
      const product = data as Record<string, unknown>;
      if (!product.mainImageUrl) {
        product.mainImageUrl = MOCK_PRODUCT_IMAGE;
      }
    }

    return NextResponse.json(data);
  } catch (error) {
    return sendErrorResponse(error);
  }
}

export async function PUT(
  request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    const body = await request.json();
    const data = await apiFetchWithRefresh(API_ENDPOINTS.products.update(id), {
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
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    await apiFetchWithRefresh(API_ENDPOINTS.products.delete(id), { method: "DELETE" });

    return new NextResponse(null, { status: 204 });
  } catch (error) {
    return sendErrorResponse(error);
  }
}
