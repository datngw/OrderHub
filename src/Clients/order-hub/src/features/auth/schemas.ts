import { z } from "zod";

const passwordMessage = "Password must be at least 8 characters with uppercase, lowercase, digit, and special character";

export const passwordSchema = z
  .string()
  .min(1, "Password is required")
  .min(8, passwordMessage)
  .regex(/[A-Z]/, passwordMessage)
  .regex(/[a-z]/, passwordMessage)
  .regex(/[0-9]/, passwordMessage)
  .regex(/[^a-zA-Z0-9]/, passwordMessage);

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Please enter a valid email address"),
  password: z
    .string()
    .min(1, "Password is required"),
});

export const registerSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Please enter a valid email address"),
  password: passwordSchema,
  fullName: z
    .string()
    .min(1, "Full name is required")
    .max(200, "Full name must not exceed 200 characters"),
  phone: z
    .string()
    .min(1, "Phone number is required")
    .max(20, "Phone must not exceed 20 characters")
    .regex(
      /^(\+84|84|0)(3[2-9]|5[6|8,9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$/,
      "Phone number must be a valid Vietnamese phone number"
    ),
});

export const forgotPasswordSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Please enter a valid email address"),
});

export const resetPasswordSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Please enter a valid email address"),
  code: z.string().min(1, "Verification code is required"),
  newPassword: passwordSchema,
});

export const changePasswordSchema = z.object({
  currentPassword: z
    .string()
    .min(1, "Current password is required"),
  newPassword: passwordSchema,
});

export type LoginFormData = z.infer<typeof loginSchema>;
export type RegisterFormData = z.infer<typeof registerSchema>;
export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;
export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;
export type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;
