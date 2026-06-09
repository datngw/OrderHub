import { z } from "zod";

export const changePasswordSchema = z.object({
  currentPassword: z.string().min(1, "Current password is required"),
  newPassword: z.string().min(1, "New password is required").min(6, "Password must be at least 6 characters"),
});

export type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;
