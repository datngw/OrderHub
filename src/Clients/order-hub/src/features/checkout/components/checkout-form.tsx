"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Banknote, Loader2, MapPin, RefreshCw, ShoppingCart } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
} from "@/components/ui/select";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";

import { useAuth } from "@/providers/auth-provider";
import { useBasket } from "@/features/basket/hooks";
import { formatPrice } from "@/lib/utils";
import { ROUTES } from "@/lib/constants";

import { checkoutSchema, type CheckoutFormData } from "../schemas";
import { createOrder } from "../api";
import { useAddress } from "../use-address";

type FieldErrors = Partial<Record<keyof CheckoutFormData, string>>;

export function CheckoutForm() {
  const router = useRouter();
  const { user } = useAuth();
  const { basket, isLoading: basketLoading, fetchBasket, clear } = useBasket();

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [pendingData, setPendingData] = useState<CheckoutFormData | null>(null);

  // Form fields
  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [phone, setPhone] = useState(user?.phone ?? "");
  const [province, setProvince] = useState("");
  const [district, setDistrict] = useState("");
  const [ward, setWard] = useState("");
  const [streetAddress, setStreetAddress] = useState("");
  const [note, setNote] = useState("");

  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const provinceId = province ? Number(province) : null;
  const districtId = district ? Number(district) : null;

  const {
    provinces,
    districts,
    wards,
    isLoading: addressLoading,
  } = useAddress(provinceId, districtId);

  // Fetch basket on mount
  useEffect(() => {
    fetchBasket();
  }, [fetchBasket]);

  function getFormData(): CheckoutFormData {
    return {
      fullName,
      email,
      phone,
      province,
      district,
      ward,
      streetAddress,
      note,
    };
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    const formData = getFormData();
    const result = checkoutSchema.safeParse(formData);

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as keyof CheckoutFormData;
        if (!errors[field]) {
          errors[field] = issue.message;
        }
      }
      setFieldErrors(errors);
      return;
    }

    if (!basket || basket.items.length === 0) {
      toast.error("Your cart is empty");
      return;
    }

    setPendingData(formData);
    setShowConfirm(true);
  }

  async function handleConfirmOrder() {
    if (!pendingData) return;

    setShowConfirm(false);
    setIsSubmitting(true);
    try {
      const order = await createOrder({
        email: pendingData.email,
        fullName: pendingData.fullName,
        phone: pendingData.phone,
        province: pendingData.province,
        district: pendingData.district,
        ward: pendingData.ward,
        streetAddress: pendingData.streetAddress,
        note: pendingData.note,
      });
      toast.success("Order placed successfully!");
      await clear();
      router.push(ROUTES.myOrderDetail(order.id));
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to place order";
      toast.error(message);
    } finally {
      setIsSubmitting(false);
      setPendingData(null);
    }
  }

  if (basketLoading) {
    return (
      <div className="flex items-center justify-center py-16">
        <RefreshCw className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!basket || basket.items.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-16 text-center">
        <ShoppingCart className="size-12 text-muted-foreground" />
        <h2 className="text-xl font-semibold">Your cart is empty</h2>
        <p className="text-muted-foreground">
          Add some products before checking out.
        </p>
        <Button
          onClick={() => router.push(ROUTES.products)}
        >
          Browse Products
        </Button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="grid gap-8 lg:grid-cols-3">
      {/* Left: Shipping Information */}
      <div className="space-y-6 lg:col-span-2">
        {/* Contact Information */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              Contact Information
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="fullName">Full Name *</Label>
                <Input
                  id="fullName"
                  placeholder="Nguyễn Văn A"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  disabled={isSubmitting}
                />
                {fieldErrors.fullName && (
                  <p className="text-xs text-destructive">
                    {fieldErrors.fullName}
                  </p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">Email *</Label>
                <Input
                  id="email"
                  type="email"
                  placeholder="email@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={isSubmitting}
                />
                {fieldErrors.email && (
                  <p className="text-xs text-destructive">
                    {fieldErrors.email}
                  </p>
                )}
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="phone">Phone Number *</Label>
              <Input
                id="phone"
                placeholder="0912345678"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                disabled={isSubmitting}
              />
              {fieldErrors.phone && (
                <p className="text-xs text-destructive">
                  {fieldErrors.phone}
                </p>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Shipping Address */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <MapPin className="size-5" />
              Shipping Address
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Province / District / Ward */}
            <div className="grid gap-4 sm:grid-cols-3">
              {/* Province */}
              <div className="space-y-2">
                <Label>Province / City *</Label>
                <Select
                  value={province || undefined}
                  onValueChange={(val) => {
                    setProvince(val ?? "");
                    setDistrict("");
                    setWard("");
                  }}
                >
                  <SelectTrigger className="w-full">
                    {province
                      ? provinces.find((p) => String(p.id) === province)?.name
                      : addressLoading ? "Loading..." : "Select province"}
                  </SelectTrigger>
                  <SelectContent>
                    {provinces.map((p) => (
                      <SelectItem key={p.id} value={String(p.id)}>
                        {p.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {fieldErrors.province && (
                  <p className="text-xs text-destructive">
                    {fieldErrors.province}
                  </p>
                )}
              </div>

              {/* District */}
              <div className="space-y-2">
                <Label>District *</Label>
                <Select
                  value={district || undefined}
                  onValueChange={(val) => {
                    setDistrict(val ?? "");
                    setWard("");
                  }}
                  disabled={!province}
                >
                  <SelectTrigger className="w-full">
                    {district
                      ? districts.find((d) => String(d.id) === district)?.name
                      : "Select district"}
                  </SelectTrigger>
                  <SelectContent>
                    {districts.map((d) => (
                      <SelectItem key={d.id} value={String(d.id)}>
                        {d.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {fieldErrors.district && (
                  <p className="text-xs text-destructive">
                    {fieldErrors.district}
                  </p>
                )}
              </div>

              {/* Ward */}
              <div className="space-y-2">
                <Label>Ward / Commune *</Label>
                <Select
                  value={ward || undefined}
                  onValueChange={(val) => setWard(val ?? "")}
                  disabled={!district}
                >
                  <SelectTrigger className="w-full">
                    {ward
                      ? wards.find((w) => String(w.id) === ward)?.name
                      : "Select ward"}
                  </SelectTrigger>
                  <SelectContent>
                    {wards.map((w) => (
                      <SelectItem key={w.id} value={String(w.id)}>
                        {w.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {fieldErrors.ward && (
                  <p className="text-xs text-destructive">
                    {fieldErrors.ward}
                  </p>
                )}
              </div>
            </div>

            {/* Street Address */}
            <div className="space-y-2">
              <Label htmlFor="streetAddress">Street Address *</Label>
              <Input
                id="streetAddress"
                placeholder="Số nhà, tên đường, khu phố..."
                value={streetAddress}
                onChange={(e) => setStreetAddress(e.target.value)}
                disabled={isSubmitting}
              />
              {fieldErrors.streetAddress && (
                <p className="text-xs text-destructive">
                  {fieldErrors.streetAddress}
                </p>
              )}
            </div>

            {/* Note */}
            <div className="space-y-2">
              <Label htmlFor="note">Order Note</Label>
              <Textarea
                id="note"
                placeholder="Ghi chú đơn hàng (tùy chọn)..."
                rows={3}
                value={note}
                onChange={(e) => setNote(e.target.value)}
                disabled={isSubmitting}
              />
            </div>
          </CardContent>
        </Card>

        {/* Payment Method */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Banknote className="size-5" />
              Payment Method
            </CardTitle>
          </CardHeader>
          <CardContent>
            <label
              className="flex cursor-pointer items-center gap-3 rounded-lg border-2 border-primary bg-primary/5 p-4 transition-colors"
            >
              <input
                type="radio"
                name="paymentMethod"
                value="COD"
                defaultChecked
                className="accent-primary"
              />
              <div className="flex-1">
                <p className="font-medium">Cash on Delivery (COD)</p>
                <p className="text-sm text-muted-foreground">
                  Pay with cash when your order is delivered
                </p>
              </div>
              <Banknote className="size-5 text-primary" />
            </label>
          </CardContent>
        </Card>
      </div>

      {/* Right: Order Summary */}
      <div>
        <Card className="sticky top-24">
          <CardHeader>
            <CardTitle>Order Summary</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Cart items */}
            <div className="space-y-3">
              {basket.items.map((item) => (
                <div
                  key={item.productId}
                  className="flex items-start justify-between gap-2 text-sm"
                >
                  <div className="flex-1">
                    <p className="font-medium line-clamp-1">
                      {item.productName}
                    </p>
                    <p className="text-muted-foreground">
                      {formatPrice(item.unitPrice)} x {item.quantity}
                    </p>
                  </div>
                  <p className="font-medium whitespace-nowrap">
                    {formatPrice(item.lineTotal)}
                  </p>
                </div>
              ))}
            </div>

            <Separator />

            <div className="flex justify-between text-lg font-semibold">
              <span>Total</span>
              <span>{formatPrice(basket.totalAmount)}</span>
            </div>
          </CardContent>
          <CardFooter>
            <Button
              type="submit"
              size="lg"
              className="w-full cursor-pointer"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 size-4 animate-spin" />
                  Placing Order...
                </>
              ) : (
                <>
                  <ShoppingCart className="mr-2 size-4" />
                  Place Order
                </>
              )}
            </Button>
          </CardFooter>
        </Card>
      </div>

      {/* Order Confirmation Dialog */}
      <ConfirmDialog
        open={showConfirm}
        onOpenChange={setShowConfirm}
        title="Place Order"
        description="Are you sure you want to place this order?"
        confirmLabel="Place Order"
        cancelLabel="Go Back"
        onConfirm={handleConfirmOrder}
      />
    </form>
  );
}
