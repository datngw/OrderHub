"use client";

import { useState } from "react";
import { z } from "zod";
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
import { forgotPasswordApi, ApiFormError } from "@/features/auth/api";
import { forgotPasswordSchema } from "@/features/auth/schemas";
import { ROUTES } from "@/lib/constants";
import Link from "next/link";
import { CheckCircle } from "lucide-react";

type FieldErrors = Partial<Record<"email", string>>;

export function ForgotPasswordForm() {
  const [email, setEmail] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSent, setIsSent] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFieldErrors({});
    setGeneralError("");

    const result = forgotPasswordSchema.safeParse({ email });

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as "email";
        if (!errors[field]) {
          errors[field] = issue.message;
        }
      }
      setFieldErrors(errors);
      return;
    }

    setIsLoading(true);

    try {
      await forgotPasswordApi(email);
      setIsSent(true);
    } catch (err) {
      if (err instanceof ApiFormError && err.errors) {
        const errors: FieldErrors = {};
        for (const [field, messages] of Object.entries(err.errors)) {
          if (!errors[field as "email"]) {
            errors[field as "email"] = messages[0];
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

  if (isSent) {
    return (
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mx-auto mb-2 flex size-12 items-center justify-center rounded-full bg-primary/10">
            <CheckCircle className="size-6 text-primary" />
          </div>
          <CardTitle className="text-2xl font-bold">Check Your Email</CardTitle>
          <CardDescription>
            We&apos;ve sent a reset code to <strong>{email}</strong>
          </CardDescription>
        </CardHeader>
        <CardFooter className="flex flex-col gap-3">
          <Link
            href={`${ROUTES.resetPassword}?email=${encodeURIComponent(email)}`}
            className="inline-flex h-10 w-full items-center justify-center rounded-md bg-primary text-primary-foreground font-medium hover:bg-primary/90 cursor-pointer"
          >
            Enter Reset Code
          </Link>
          <p className="text-sm text-muted-foreground">
            Didn&apos;t receive the code?{" "}
            <button
              type="button"
              className="text-foreground hover:underline"
              onClick={() => setIsSent(false)}
            >
              Try again
            </button>
          </p>
        </CardFooter>
      </Card>
    );
  }

  return (
    <Card className="w-full max-w-md">
      <CardHeader className="text-center">
        <CardTitle className="text-2xl font-bold">Forgot Password</CardTitle>
        <CardDescription>
          Enter your email and we&apos;ll send you a reset code
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
              disabled={isLoading}
              autoComplete="email"
            />
            {fieldErrors.email && (
              <p className="text-sm text-destructive">{fieldErrors.email}</p>
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
            {isLoading ? "Sending..." : "Send Reset Code"}
          </Button>
          <Link
            href={ROUTES.login}
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            Back to sign in
          </Link>
        </CardFooter>
      </form>
    </Card>
  );
}
