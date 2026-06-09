import type { Metadata } from "next";
import { LoginForm } from "@/features/auth/components/login-form";

export const metadata: Metadata = {
  title: "Sign In - Order Hub",
  description: "Sign in to your Order Hub account",
};

export default function LoginPage() {
  return <LoginForm />;
}
