import "server-only";

import { apiFetch } from "@/lib/api-client";
import { getAccessToken } from "@/lib/auth";
import { ApiError } from "@/types/common";

export async function apiFetchWithRefresh<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const accessToken = await getAccessToken();

  if (!accessToken) {
    throw new ApiError(401, "Unauthorized");
  }

  return await apiFetch<T>(path, options, accessToken);
}

export async function apiFetchOptionalAuth<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const accessToken = await getAccessToken();

  if (!accessToken) {
    return await apiFetch<T>(path, options);
  }

  return await apiFetch<T>(path, options, accessToken);
}
