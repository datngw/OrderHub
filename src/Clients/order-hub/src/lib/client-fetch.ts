import { ROUTES } from "@/lib/constants";
import { ApiError } from "@/types/common";

const USER_STORAGE_KEY = "orderhub_user";

export async function throwIfNotOk(response: Response): Promise<void> {
  if (response.ok) return;

  if (response.status === 401) {
    if (
      typeof window !== "undefined" &&
      !window.location.pathname.startsWith(ROUTES.login) &&
      !window.location.pathname.startsWith(ROUTES.register)
    ) {
      localStorage.removeItem(USER_STORAGE_KEY);
      window.location.href = ROUTES.login;
    }
    throw new ApiError(401, "Session expired");
  }

  let message = "An unexpected error occurred";
  let errors: Record<string, string[]> | undefined;

  try {
    const body = await response.json();
    message = body.detail || body.title || body.message || message;
    errors = body.errors;
  } catch {
    // Body empty or not JSON
  }

  throw new ApiError(response.status, message, errors);
}

let refreshPromise: Promise<boolean> | null = null;

async function tryClientRefresh(): Promise<boolean> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    try {
      const response = await fetch("/api/auth/refresh", { 
        method: "POST",
        signal: AbortSignal.timeout(8000)
      });
      return response.ok;
    } catch {
      return false;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}

export async function clientFetch<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const timeoutSignal = AbortSignal.timeout(8000);
  const signal = options.signal
    ? AbortSignal.any([options.signal, timeoutSignal])
    : timeoutSignal;

  let response = await fetch(path, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...options.headers,
    },
    signal,
  });

  if (response.status === 401) {
    const refreshed = await tryClientRefresh();

    if (refreshed) {
      response = await fetch(path, {
        ...options,
        headers: {
          "Content-Type": "application/json",
          ...options.headers,
        },
        signal,
      });

      if (response.ok) {
        if (response.status === 204) return undefined as T;
        return response.json();
      }

      if (response.status !== 401) {
        await throwIfNotOk(response);
      }
    }

    if (
      typeof window !== "undefined" &&
      !window.location.pathname.startsWith(ROUTES.login) &&
      !window.location.pathname.startsWith(ROUTES.register)
    ) {
      localStorage.removeItem(USER_STORAGE_KEY);
      window.location.href = ROUTES.login;
    }
    throw new ApiError(401, "Session expired");
  }

  await throwIfNotOk(response);

  if (response.status === 204) return undefined as T;
  return response.json();
}
