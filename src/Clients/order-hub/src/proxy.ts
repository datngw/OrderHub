import { NextRequest, NextResponse } from "next/server";
import { env } from "@/lib/env";
import { API_ENDPOINTS } from "@/lib/api-endpoints";

const publicPaths = ["/login", "/register", "/forgot-password", "/reset-password"];
const apiPublicPaths = [
  "/api/auth/login",
  "/api/auth/register",
  "/api/auth/refresh",
  "/api/auth/forgot-password",
  "/api/auth/reset-password",
  "/api/products",
];

const API_BASE_URL = env.API_BASE_URL;

const COOKIE_OPTIONS = {
  httpOnly: true,
  secure: env.NODE_ENV === "production",
  sameSite: "lax" as const,
  path: "/",
};

/**
 * Decode role from JWT access token (base64url decode payload only).
 * No signature verification here — backend API enforces real auth.
 * This is just for client-side route protection (defense in depth).
 */
function decodeJwtRole(token: string): string | null {
  try {
    const parts = token.split(".");
    if (parts.length !== 3) return null;

    // base64url → base64
    let payload = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    while (payload.length % 4) payload += "=";

    const decoded = JSON.parse(
      Buffer.from(payload, "base64").toString("utf-8")
    );

    // ASP.NET Core JWT: short "role" claim or long ClaimTypes.Role URI
    return (
      decoded.role ??
      decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ??
      null
    );
  } catch {
    return null;
  }
}

/**
 * Check if a path requires Admin role.
 */
function isAdminRoute(pathname: string): boolean {
  return pathname.startsWith("/admin");
}

/**
 * Get redirect destination based on user role.
 */
function getHomeForRole(role: string | null): string {
  return role === "Admin" ? "/admin/dashboard" : "/";
}

/**
 * Next.js Proxy — route protection with role-based access + silent token refresh.
 *
 * Flow:
 * 1. Public paths → next (redirect away if authenticated)
 * 2. Admin routes → require Admin role
 * 3. Protected path + access_token exists → decode role from JWT → allow/deny
 * 4. Protected path + access_token missing + refresh_token exists
 *    → silent refresh → check role → allow/deny
 * 5. Protected path + no tokens → redirect to login
 */
export async function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const accessToken = request.cookies.get("access_token")?.value;
  const refreshToken = request.cookies.get("refresh_token")?.value;

  // ---- Public API routes ----
  if (apiPublicPaths.some((p) => pathname.startsWith(p))) {
    return NextResponse.next();
  }

  // ---- Public auth pages (login, register, forgot-password, reset-password) ----
  if (publicPaths.some((p) => pathname.startsWith(p))) {
    if (accessToken) {
      const role = decodeJwtRole(accessToken);
      return NextResponse.redirect(new URL(getHomeForRole(role), request.url));
    }
    // Even without access_token, if refresh_token exists, user might be logged in
    // → let them see the login page; the auth-provider will restore session client-side
    return NextResponse.next();
  }

  // ---- Public product pages (browseable without login) ----
  if (pathname === "/products" || pathname.match(/^\/products\/[^/]+$/)) {
    return NextResponse.next();
  }

  // ---- Home page is public ----
  if (pathname === "/") {
    return NextResponse.next();
  }

  // ---- Static assets ----
  if (
    pathname.startsWith("/_next") ||
    pathname.startsWith("/favicon") ||
    pathname.includes(".")
  ) {
    return NextResponse.next();
  }

  // ---- Protected routes ----

  const requiresAdmin = isAdminRoute(pathname);

  // Case 1: access_token exists → decode role from JWT
  if (accessToken) {
    const role = decodeJwtRole(accessToken);

    if (requiresAdmin && role !== "Admin") {
      return NextResponse.redirect(new URL("/", request.url));
    }

    return NextResponse.next();
  }

  // Case 2: access_token expired but refresh_token exists → silent refresh
  if (refreshToken) {
    try {
      const refreshResponse = await fetch(
        `${API_BASE_URL}${API_ENDPOINTS.auth.refresh}`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken }),
        }
      );

      if (refreshResponse.ok) {
        const data = await refreshResponse.json();

        // Check role from the refreshed token
        const role = decodeJwtRole(data.accessToken);

        if (requiresAdmin && role !== "Admin") {
          // Set new cookies anyway (session is valid) but deny admin access
          const response = NextResponse.redirect(new URL("/", request.url));
          response.cookies.set("access_token", data.accessToken, {
            ...COOKIE_OPTIONS,
            expires: new Date(data.accessTokenExpiresAt),
          });
          response.cookies.set("refresh_token", data.refreshToken, {
            ...COOKIE_OPTIONS,
            expires: new Date(data.refreshTokenExpiresAt),
          });
          return response;
        }

        // Set new cookies and allow request
        const requestHeaders = new Headers(request.headers);
        requestHeaders.set("x-access-token", data.accessToken);
        requestHeaders.set("Authorization", `Bearer ${data.accessToken}`);

        const response = NextResponse.next({
          request: {
            headers: requestHeaders,
          },
        });
        response.cookies.set("access_token", data.accessToken, {
          ...COOKIE_OPTIONS,
          expires: new Date(data.accessTokenExpiresAt),
        });
        response.cookies.set("refresh_token", data.refreshToken, {
          ...COOKIE_OPTIONS,
          expires: new Date(data.refreshTokenExpiresAt),
        });
        return response;
      }
    } catch {
      // Refresh failed → fall through to login redirect
    }
  }

  // Case 3: No valid tokens → redirect to login
  const loginUrl = new URL("/login", request.url);
  loginUrl.searchParams.set("callbackUrl", pathname);

  const response = NextResponse.redirect(loginUrl);
  response.cookies.delete("access_token");
  response.cookies.delete("refresh_token");
  return response;
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico|logo.svg).*)"],
};
