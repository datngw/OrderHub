import type { Metadata } from "next";
import { RegisterForm } from "@/features/auth/components/register-form";

export const metadata: Metadata = {
  title: "Register - Order Hub",
  description: "Create your Order Hub account",
};

export default function RegisterPage() {
  return <RegisterForm />;
}
