"use client";

import { useState } from "react";
import { useAuth } from "@/providers/auth-provider";
import { PageHeader } from "@/components/shared/page-header";
import {
  Card,
  CardContent,
  CardHeader,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/shared/password-input";
import { User, Mail, Phone, Settings, Lock } from "lucide-react";
import { toast } from "sonner";
import { changePasswordApi } from "@/features/profile/api";
import { z } from "zod";

const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "Current password is required"),
    newPassword: z.string().min(1, "New password is required"),
    confirmPassword: z.string().min(1, "Please confirm your new password"),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

type FieldErrors = Partial<Record<string, string>>;

function ProfileTab() {
  const { user } = useAuth();

  if (!user) return null;

  const roleLabel = user.role === "Admin" ? "Administrator" : "Customer";

  return (
    <Card>
      <CardHeader className="items-center text-center">
        <div className="mt-3">
          <h2 className="text-xl font-semibold">{user.fullName}</h2>
          <Badge variant="secondary" className="mt-1">
            {roleLabel}
          </Badge>
        </div>
      </CardHeader>
      <Separator />
      <CardContent className="space-y-5 pt-6">
        <div className="flex items-center gap-3">
          <User className="size-4 text-muted-foreground" />
          <div>
            <p className="text-sm text-muted-foreground">Full Name</p>
            <p className="font-medium">{user.fullName}</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <Mail className="size-4 text-muted-foreground" />
          <div>
            <p className="text-sm text-muted-foreground">Email</p>
            <p className="font-medium">{user.email}</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <Phone className="size-4 text-muted-foreground" />
          <div>
            <p className="text-sm text-muted-foreground">Phone</p>
            <p className="font-medium">{user.phone || "Not updated"}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function ChangePasswordTab() {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFieldErrors({});
    setGeneralError("");

    const result = changePasswordSchema.safeParse({
      currentPassword,
      newPassword,
      confirmPassword,
    });

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as string;
        if (!errors[field]) {
          errors[field] = issue.message;
        }
      }
      setFieldErrors(errors);
      return;
    }

    setIsLoading(true);

    try {
      await changePasswordApi(currentPassword, newPassword);
      toast.success("Password changed successfully");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (err) {
      setGeneralError(
        err instanceof Error ? err.message : "An error occurred"
      );
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Lock className="size-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold">Change Password</h2>
        </div>
      </CardHeader>
      <Separator />
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4 pt-6">
          <div className="space-y-2">
            <Label htmlFor="currentPassword">Current Password</Label>
            <PasswordInput
              id="currentPassword"
              placeholder="Enter current password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              disabled={isLoading}
              autoComplete="current-password"
            />
            {fieldErrors.currentPassword && (
              <p className="text-sm text-destructive">
                {fieldErrors.currentPassword}
              </p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="newPassword">New Password</Label>
            <PasswordInput
              id="newPassword"
              placeholder="Enter new password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              disabled={isLoading}
              autoComplete="new-password"
            />
            {fieldErrors.newPassword && (
              <p className="text-sm text-destructive">
                {fieldErrors.newPassword}
              </p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">Confirm New Password</Label>
            <PasswordInput
              id="confirmPassword"
              placeholder="Re-enter new password"
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
          <Button
            type="submit"
            className="w-full cursor-pointer"
            disabled={isLoading}
          >
            {isLoading ? "Processing..." : "Change Password"}
          </Button>
        </CardContent>
      </form>
    </Card>
  );
}

export default function ProfilePage() {
  const [activeTab, setActiveTab] = useState("profile");

  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <PageHeader
        title="Settings"
        description="Manage your account information"
      />

      <div className="mx-auto mt-6 max-w-2xl">
        <Tabs
          value={activeTab}
          onValueChange={setActiveTab}
        >
          <TabsList>
            <TabsTrigger value="profile" className="gap-1.5">
              <User className="size-4" />
              Profile
            </TabsTrigger>
            <TabsTrigger value="password" className="gap-1.5">
              <Lock className="size-4" />
              Password
            </TabsTrigger>
          </TabsList>
          <TabsContent value="profile" className="mt-6">
            <ProfileTab />
          </TabsContent>
          <TabsContent value="password" className="mt-6">
            <ChangePasswordTab />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
