"use client";

import { useState } from "react";
import { useSearchParams } from "next/navigation";
import { z } from "zod";
import { CircleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
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
import { PasswordInput } from "@/components/shared/password-input";
import { resetPasswordApi, ApiFormError } from "@/features/auth/api";
import { resetPasswordSchema } from "@/features/auth/schemas";
import { ROUTES } from "@/lib/constants";
import Link from "next/link";

type ResetFormValues = z.infer<typeof resetPasswordSchema> & { confirmPassword: string };
type FieldErrors = Partial<Record<keyof ResetFormValues, string>>;

export function ResetPasswordForm() {
  const searchParams = useSearchParams();
  const prefilledEmail = searchParams.get("email") ?? "";
  const [email, setEmail] = useState(prefilledEmail);
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFieldErrors({});
    setGeneralError("");

    const result = resetPasswordSchema.safeParse({ email, code, newPassword });

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as keyof ResetFormValues;
        if (!errors[field]) {
          errors[field] = issue.message;
        }
      }
      setFieldErrors(errors);
      return;
    }

    if (newPassword !== confirmPassword) {
      setFieldErrors({ confirmPassword: "Passwords do not match" });
      return;
    }

    setIsLoading(true);

    try {
      await resetPasswordApi({ email, code, newPassword });
      setIsSuccess(true);
    } catch (err) {
      if (err instanceof ApiFormError && err.errors) {
        const errors: FieldErrors = {};
        for (const [field, messages] of Object.entries(err.errors)) {
          const key = field.charAt(0).toLowerCase() + field.slice(1) as keyof ResetFormValues;
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

  if (isSuccess) {
    return (
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl font-bold">Password Reset</CardTitle>
          <CardDescription>
            Your password has been reset successfully.
          </CardDescription>
        </CardHeader>
        <CardFooter>
          <Link
            href={ROUTES.login}
            className="inline-flex h-10 w-full items-center justify-center rounded-md bg-primary text-primary-foreground font-medium hover:bg-primary/90 cursor-pointer"
          >
            Sign In
          </Link>
        </CardFooter>
      </Card>
    );
  }

  return (
    <Card className="w-full max-w-md">
      <CardHeader className="text-center">
        <CardTitle className="text-2xl font-bold">Reset Password</CardTitle>
        <CardDescription>
          Enter the code from your email and set a new password
        </CardDescription>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="email">
              Email <span className="text-destructive">*</span>
            </Label>
            <Input
              id="email"
              type="email"
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              readOnly={!!prefilledEmail}
              disabled={isLoading}
              autoComplete="email"
            />
            {fieldErrors.email && (
              <p className="text-sm text-destructive">{fieldErrors.email}</p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="code">
              Verification Code <span className="text-destructive">*</span>
            </Label>
            <Input
              id="code"
              type="text"
              placeholder="Enter the code from your email"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              disabled={isLoading}
              autoComplete="one-time-code"
            />
            {fieldErrors.code && (
              <p className="text-sm text-destructive">{fieldErrors.code}</p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="newPassword">
              New Password <span className="text-destructive">*</span>
              <Tooltip>
                <TooltipTrigger className="ml-1 cursor-help">
                  <CircleAlert className="size-3.5 text-muted-foreground" />
                </TooltipTrigger>
                <TooltipContent>
                  At least 8 characters · Uppercase, lowercase, digit, special character
                </TooltipContent>
              </Tooltip>
            </Label>
            <PasswordInput
              id="newPassword"
              placeholder="At least 8 characters"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              disabled={isLoading}
              autoComplete="new-password"
            />
            {fieldErrors.newPassword && (
              <p className="text-sm text-destructive">{fieldErrors.newPassword}</p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">
              Confirm New Password <span className="text-destructive">*</span>
            </Label>
            <PasswordInput
              id="confirmPassword"
              placeholder="Re-enter your new password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              disabled={isLoading}
              autoComplete="new-password"
            />
            {fieldErrors.confirmPassword && (
              <p className="text-sm text-destructive">{fieldErrors.confirmPassword}</p>
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
            {isLoading ? "Resetting..." : "Reset Password"}
          </Button>
          <Link
            href={ROUTES.forgotPassword}
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            Didn&apos;t get a code? Resend
          </Link>
        </CardFooter>
      </form>
    </Card>
  );
}
