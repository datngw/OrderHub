import { env } from "@/lib/env";
import { ApiError } from "@/types/common";
import type { ApiErrorResponse } from "@/types/auth";

const API_BASE_URL = env.API_BASE_URL;

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
  accessToken?: string
): Promise<T> {
  const url = `${API_BASE_URL}${path}`;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };

  if (accessToken) {
    headers["Authorization"] = `Bearer ${accessToken}`;
  }

  const timeoutSignal = AbortSignal.timeout(8000);
  const signal = options.signal
    ? AbortSignal.any([options.signal, timeoutSignal])
    : timeoutSignal;

  const response = await fetch(url, {
    ...options,
    headers,
    signal,
  });

  if (!response.ok) {
    if (response.status === 204) return undefined as T;

    const errorBody: ApiErrorResponse = await response.json().catch(() => ({
      detail: "An unexpected error occurred",
    }));

    throw new ApiError(
      response.status,
      errorBody.detail || errorBody.title || "An unexpected error occurred",
      errorBody.errors
    );
  }

  if (response.status === 204) return undefined as T;

  return response.json();
}
