import { NextRequest, NextResponse } from "next/server";
import { uploadToCloudinary } from "@/lib/cloudinary";
import { sendErrorResponse } from "@/lib/error-handler";

export async function POST(request: NextRequest) {
  try {
    const formData = await request.formData();
    const file = formData.get("file") as File | null;

    if (!file) {
      return NextResponse.json({ message: "No file provided" }, { status: 400 });
    }

    const result = await uploadToCloudinary(file);
    return NextResponse.json(result);
  } catch (error) {
    return sendErrorResponse(error);
  }
}
