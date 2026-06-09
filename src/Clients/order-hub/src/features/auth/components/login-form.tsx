"use client";

import { useState } from "react";
import { z } from "zod";
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
import { useAuth } from "@/providers/auth-provider";
import { ApiFormError } from "@/features/auth/api";
import { loginSchema } from "@/features/auth/schemas";
import { ROUTES } from "@/lib/constants";
import Link from "next/link";

type LoginFormValues = z.infer<typeof loginSchema>;
type FieldErrors = Partial<Record<keyof LoginFormValues, string>>;

export function LoginForm() {
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFieldErrors({});
    setGeneralError("");

    const result = loginSchema.safeParse({ email, password });

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as keyof LoginFormValues;
        if (!errors[field]) {
          errors[field] = issue.message;
        }
      }
      setFieldErrors(errors);
      return;
    }

    setIsLoading(true);

    try {
      await login(email, password);
    } catch (err) {
      if (err instanceof ApiFormError && err.errors) {
        const errors: FieldErrors = {};
        for (const [field, messages] of Object.entries(err.errors)) {
          const key = field.charAt(0).toLowerCase() + field.slice(1) as keyof LoginFormValues;
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
        <CardTitle className="text-2xl font-bold">Sign In</CardTitle>
        <CardDescription>
          Enter your credentials to access Order Hub
        </CardDescription>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
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
            <Label htmlFor="password">Password</Label>
            <PasswordInput
              id="password"
              placeholder="Enter your password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isLoading}
              autoComplete="current-password"
            />
            {fieldErrors.password && (
              <p className="text-sm text-destructive">
                {fieldErrors.password}
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
            {isLoading ? "Signing in..." : "Sign In"}
          </Button>
          <div className="flex w-full justify-between text-sm">
            <Link
              href={ROUTES.register}
              className="text-muted-foreground hover:text-foreground"
            >
              Create account
            </Link>
            <Link
              href={ROUTES.forgotPassword}
              className="text-muted-foreground hover:text-foreground"
            >
              Forgot password?
            </Link>
          </div>
        </CardFooter>
      </form>
    </Card>
  );
}
