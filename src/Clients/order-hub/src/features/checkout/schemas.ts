import { z } from "zod";

export const checkoutSchema = z.object({
  email: z.string().min(1, "Email is required").email("Invalid email"),
  fullName: z.string().min(1, "Full name is required").min(2, "Name must be at least 2 characters"),
  phone: z.string().min(1, "Phone is required"),
  province: z.string().min(1, "Province is required"),
  district: z.string().min(1, "District is required"),
  ward: z.string().min(1, "Ward is required"),
  streetAddress: z.string().min(1, "Street address is required"),
  note: z.string().optional(),
});

export type CheckoutFormData = z.infer<typeof checkoutSchema>;
