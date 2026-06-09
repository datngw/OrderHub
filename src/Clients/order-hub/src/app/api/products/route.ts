import { NextRequest, NextResponse } from "next/server";
import { apiFetchWithRefresh, apiFetchOptionalAuth } from "@/lib/api-with-refresh";
import { API_ENDPOINTS } from "@/lib/api-endpoints";
import { sendErrorResponse } from "@/lib/error-handler";

/** Mock product image — replace with real images in production */
const MOCK_PRODUCT_IMAGE = "/products/sennheiser-headphone.jpg";

function injectMockImages(data: unknown): unknown {
  if (!data || typeof data !== "object") return data;

  // Handle paged result: { items: [...], ... }
  const obj = data as Record<string, unknown>;
  if (Array.isArray(obj.items)) {
    obj.items = obj.items.map((product: Record<string, unknown>) => ({
      ...product,
      mainImageUrl: product.mainImageUrl || MOCK_PRODUCT_IMAGE,
    }));
    return obj;
  }

  // Handle plain array
  if (Array.isArray(data)) {
    return (data as Record<string, unknown>[]).map((product) => ({
      ...product,
      mainImageUrl: product.mainImageUrl || MOCK_PRODUCT_IMAGE,
    }));
  }

  return data;
}

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const isAdmin = searchParams.get("admin") === "true";

    // Build backend query params (remove our internal "admin" flag)
    const backendParams = new URLSearchParams();
    searchParams.forEach((value, key) => {
      if (key !== "admin") backendParams.set(key, value);
    });

    const path = isAdmin ? API_ENDPOINTS.products.adminList : API_ENDPOINTS.products.list;
    const qs = backendParams.toString();
    const fetcher = isAdmin ? apiFetchWithRefresh : apiFetchOptionalAuth;
    const data = await fetcher(`${path}${qs ? `?${qs}` : ""}`);

    return NextResponse.json(injectMockImages(data));
  } catch (error) {
    return sendErrorResponse(error);
  }
}

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const data = await apiFetchWithRefresh(API_ENDPOINTS.products.create, {
      method: "POST",
      body: JSON.stringify(body),
    });

    return NextResponse.json(data, { status: 201 });
  } catch (error) {
    return sendErrorResponse(error);
  }
}
