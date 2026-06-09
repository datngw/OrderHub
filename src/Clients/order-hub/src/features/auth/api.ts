import type { UserInfo } from "@/types/auth";
import { throwIfNotOk } from "@/lib/client-fetch";

// Re-export for backward compatibility
export { ApiError as ApiFormError } from "@/types/common";

export async function loginApi(
  email: string,
  password: string
): Promise<{ user: UserInfo }> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  const data = await response.json();
  await throwIfNotOk(response);
  return data;
}

export async function registerApi(data: {
  email: string;
  password: string;
  fullName: string;
  phone: string;
}): Promise<{ user: UserInfo }> {
  const response = await fetch("/api/auth/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  const result = await response.json();
  await throwIfNotOk(response);
  return result;
}

export async function logoutApi(refreshToken: string): Promise<void> {
  await fetch("/api/auth/logout", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });
}

export async function forgotPasswordApi(email: string): Promise<void> {
  const response = await fetch("/api/auth/forgot-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });
  await throwIfNotOk(response);
}

export async function resetPasswordApi(data: {
  email: string;
  code: string;
  newPassword: string;
}): Promise<void> {
  const response = await fetch("/api/auth/reset-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  await throwIfNotOk(response);
}

export async function changePasswordApi(data: {
  currentPassword: string;
  newPassword: string;
}): Promise<void> {
  const response = await fetch("/api/auth/change-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  await throwIfNotOk(response);
}
