"use client";

import { useState } from "react";
import { z } from "zod";
import { CircleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/shared/password-input";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useAuth } from "@/providers/auth-provider";
import { ApiFormError } from "@/features/auth/api";
import { registerSchema } from "@/features/auth/schemas";
import { ROUTES } from "@/lib/constants";
import Link from "next/link";

const registerFormSchema = registerSchema.extend({
  confirmPassword: z.string().min(1, "Please confirm your password"),
});

type RegisterFormValues = z.infer<typeof registerFormSchema>;
type FieldErrors = Partial<Record<keyof RegisterFormValues, string>>;

function FieldTooltip({ hints }: { hints: string[] }) {
  return (
    <Tooltip>
      <TooltipTrigger className="ml-1 cursor-help">
        <CircleAlert className="size-3.5 text-muted-foreground" />
      </TooltipTrigger>
      <TooltipContent>
        {hints.join(" · ")}
      </TooltipContent>
    </Tooltip>
  );
}

export function RegisterForm() {
  const { register } = useAuth();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFieldErrors({});
    setGeneralError("");

    const formData = { fullName, email, phone, password, confirmPassword };

    const result = registerFormSchema.safeParse(formData);

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as keyof RegisterFormValues;
        if (!errors[field]) {
          errors[field] = issue.message;
        }
      }
      setFieldErrors(errors);
      return;
    }

    if (password !== confirmPassword) {
      setFieldErrors({ confirmPassword: "Passwords do not match" });
      return;
    }

    setIsLoading(true);

    try {
      await register({ email, password, fullName, phone });
    } catch (err) {
      if (err instanceof ApiFormError && err.errors) {
        const errors: FieldErrors = {};
        for (const [field, messages] of Object.entries(err.errors)) {
          const key = field.charAt(0).toLowerCase() + field.slice(1) as keyof RegisterFormValues;
          if (!errors[key]) {
            errors[key] = messages[0];
          }
        }
        setFieldErrors(errors);
      }
      setGeneralError(
        err instanceof Error ? err.message : "Something went wrong"
      );
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <Card className="w-full max-w-md">
      <CardHeader className="text-center">
        <CardTitle className="text-2xl font-bold">Create Account</CardTitle>
        <CardDescription>
          Register to start shopping on Order Hub
        </CardDescription>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="fullName">
              Full Name <span className="text-destructive">*</span>
              <FieldTooltip hints={["Max 200 characters"]} />
            </Label>
            <Input
              id="fullName"
              type="text"
              placeholder="John Doe"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              disabled={isLoading}
              autoComplete="name"
            />
            {fieldErrors.fullName && (
              <p className="text-sm text-destructive">{fieldErrors.fullName}</p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="email">
              Email <span className="text-destructive">*</span>
              <FieldTooltip hints={["Must be a valid email address"]} />
            </Label>
            <Input
              id="email"
              type="email"
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={isLoading}
              autoComplete="email"
            />
            {fieldErrors.email && (
              <p className="text-sm text-destructive">{fieldErrors.email}</p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="phone">
              Phone <span className="text-destructive">*</span>
              <FieldTooltip
                hints={["Valid Vietnamese phone number", "e.g. 0912345678"]}
              />
            </Label>
            <Input
              id="phone"
              type="tel"
              placeholder="0912345678"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              disabled={isLoading}
              autoComplete="tel"
            />
            {fieldErrors.phone && (
              <p className="text-sm text-destructive">{fieldErrors.phone}</p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="password">
              Password <span className="text-destructive">*</span>
              <FieldTooltip
                hints={[
                  "At least 8 characters",
                  "Uppercase, lowercase, digit, special character",
                ]}
              />
            </Label>
            <PasswordInput
              id="password"
              placeholder="At least 8 characters"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isLoading}
              autoComplete="new-password"
            />
            {fieldErrors.password && (
              <p className="text-sm text-destructive">
                {fieldErrors.password}
              </p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">
              Confirm Password <span className="text-destructive">*</span>
              <FieldTooltip
                hints={["Must match the password above"]}
              />
            </Label>
            <PasswordInput
              id="confirmPassword"
              placeholder="Re-enter your password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              disabled={isLoading}
              autoComplete="new-password"
            />
            {fieldErrors.confirmPassword && (
              <p className="text-sm text-destructive">
                {fieldErrors.confirmPassword}
              </p>
            )}
          </div>
          {generalError && (
            <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
              {generalError}
            </div>
          )}
        </CardContent>
        <CardFooter className="flex flex-col gap-3 mt-3">
          <Button type="submit" className="w-full cursor-pointer" disabled={isLoading}>
            {isLoading ? "Creating account..." : "Create Account"}
          </Button>
          <p className="text-sm text-muted-foreground">
            Already have an account?{" "}
            <Link
              href={ROUTES.login}
              className="text-foreground hover:underline"
            >
              Sign in
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
}
