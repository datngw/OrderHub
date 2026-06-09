import "server-only";
import { cookies, headers } from "next/headers";

const COOKIE_OPTIONS = {
  httpOnly: true,
  secure: process.env.NODE_ENV === "production",
  sameSite: "lax" as const,
  path: "/",
};

export async function setAuthCookies(loginResponse: {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}) {
  const cookieStore = await cookies();

  cookieStore.set("access_token", loginResponse.accessToken, {
    ...COOKIE_OPTIONS,
    expires: new Date(loginResponse.accessTokenExpiresAt),
  });

  cookieStore.set("refresh_token", loginResponse.refreshToken, {
    ...COOKIE_OPTIONS,
    expires: new Date(loginResponse.refreshTokenExpiresAt),
  });
}

export async function getAccessToken(): Promise<string | undefined> {
  try {
    const headerStore = await headers();
    const authHeader = headerStore.get("x-access-token") || headerStore.get("Authorization");
    if (authHeader) {
      return authHeader.startsWith("Bearer ") ? authHeader.substring(7) : authHeader;
    }
  } catch {
    // Ignored
  }

  try {
    const cookieStore = await cookies();
    return cookieStore.get("access_token")?.value;
  } catch {
    return undefined;
  }
}

export async function getRefreshToken(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get("refresh_token")?.value;
}

export async function isAuthenticated(): Promise<boolean> {
  const cookieStore = await cookies();
  return !!cookieStore.get("access_token")?.value;
}

export async function clearAuthCookies() {
  const cookieStore = await cookies();
  cookieStore.delete("access_token");
  cookieStore.delete("refresh_token");
}
