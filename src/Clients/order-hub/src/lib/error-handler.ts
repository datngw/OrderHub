import { NextResponse } from "next/server";
import { ApiError } from "@/types/common";

export function sendErrorResponse(error: unknown) {
  if (error instanceof ApiError) {
    const status = error.status >= 500 ? 500 : error.status;
    return NextResponse.json(
      {
        message: error.message,
        ...(error.errors && { errors: error.errors }),
      },
      { status }
    );
  }

  if (error instanceof SyntaxError) {
    return NextResponse.json(
      { message: "Invalid request body" },
      { status: 400 }
    );
  }

  const message = error instanceof Error ? error.message : "An unexpected error occurred";
  return NextResponse.json(
    { message },
    { status: 500 }
  );
}
