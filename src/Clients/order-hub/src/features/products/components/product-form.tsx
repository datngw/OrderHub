"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { createProductSchema } from "../schemas";
import type { CreateProductFormData } from "../schemas";
import type { Product, CreateProductRequest, UpdateProductRequest } from "../types";

type FieldErrors = Partial<Record<keyof CreateProductFormData, string>>;

const CATEGORIES = [
  "Electronics",
  "Clothing",
  "Home & Garden",
  "Sports",
  "Books",
  "Toys",
  "Food & Beverage",
  "Health & Beauty",
];

interface ProductFormProps {
  product?: Product;
  onSubmit: (data: CreateProductRequest | UpdateProductRequest) => Promise<void>;
  onCancel: () => void;
  isLoading: boolean;
}

export function ProductForm({
  product,
  onSubmit,
  onCancel,
  isLoading,
}: ProductFormProps) {
  const isEdit = !!product;

  const [sku, setSku] = useState(product?.sku ?? "");
  const [name, setName] = useState(product?.name ?? "");
  const [description, setDescription] = useState(product?.description ?? "");
  const [price, setPrice] = useState(product ? String(product.price) : "");
  const [stock, setStock] = useState(product ? String(product.stock) : "");
  const [category, setCategory] = useState(product?.category ?? "");
  const [isActive, setIsActive] = useState(product?.isActive ?? true);

  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState("");

  const [prevProduct, setPrevProduct] = useState<Product | undefined>(product);

  if (product !== prevProduct) {
    setPrevProduct(product);
    setSku(product?.sku ?? "");
    setName(product?.name ?? "");
    setDescription(product?.description ?? "");
    setPrice(product ? String(product.price) : "");
    setStock(product ? String(product.stock) : "");
    setCategory(product?.category ?? "");
    setIsActive(product?.isActive ?? true);
    setFieldErrors({});
    setGeneralError("");
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFieldErrors({});
    setGeneralError("");

    const formData = {
      sku,
      name,
      description,
      price: Number(price),
      stock: Number(stock),
      category,
      isActive,
    };

    const result = createProductSchema.safeParse(formData);

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as keyof CreateProductFormData;
        if (!errors[field]) {
          errors[field] = issue.message;
        }
      }
      setFieldErrors(errors);
      return;
    }

    try {
      if (isEdit) {
        await onSubmit({
          name,
          description,
          price: Number(price),
          stock: Number(stock),
          category,
          isActive,
        });
      } else {
        await onSubmit(formData as CreateProductRequest);
      }
    } catch (err) {
      setGeneralError(
        err instanceof Error ? err.message : "Something went wrong"
      );
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {generalError && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {generalError}
        </div>
      )}

      <div className="grid grid-cols-2 gap-4">
        {/* SKU */}
        <div className="space-y-2">
          <Label htmlFor="sku">SKU</Label>
          <Input
            id="sku"
            value={sku}
            onChange={(e) => setSku(e.target.value)}
            disabled={isLoading || isEdit}
            placeholder="PRD-001"
          />
          {fieldErrors.sku && (
            <p className="text-sm text-destructive">{fieldErrors.sku}</p>
          )}
        </div>

        {/* Name */}
        <div className="space-y-2">
          <Label htmlFor="name">Name</Label>
          <Input
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={isLoading}
            placeholder="Product name"
          />
          {fieldErrors.name && (
            <p className="text-sm text-destructive">{fieldErrors.name}</p>
          )}
        </div>
      </div>

      {/* Description */}
      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <Textarea
          id="description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          disabled={isLoading}
          placeholder="Product description"
          rows={3}
        />
        {fieldErrors.description && (
          <p className="text-sm text-destructive">{fieldErrors.description}</p>
        )}
      </div>

      {/* Price + Stock */}
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="price">Price (VND)</Label>
          <Input
            id="price"
            type="number"
            value={price}
            onChange={(e) => setPrice(e.target.value)}
            disabled={isLoading}
            placeholder="0"
            min="0"
          />
          {fieldErrors.price && (
            <p className="text-sm text-destructive">{fieldErrors.price}</p>
          )}
        </div>
        <div className="space-y-2">
          <Label htmlFor="stock">Stock</Label>
          <Input
            id="stock"
            type="number"
            value={stock}
            onChange={(e) => setStock(e.target.value)}
            disabled={isLoading}
            placeholder="0"
            min="0"
          />
          {fieldErrors.stock && (
            <p className="text-sm text-destructive">{fieldErrors.stock}</p>
          )}
        </div>
      </div>

      {/* Category */}
      <div className="space-y-2">
        <Label>Category</Label>
        <Select value={category} onValueChange={(v) => v && setCategory(v)}>
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Select category" />
          </SelectTrigger>
          <SelectContent>
            {CATEGORIES.map((cat) => (
              <SelectItem key={cat} value={cat}>
                {cat}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {fieldErrors.category && (
          <p className="text-sm text-destructive">{fieldErrors.category}</p>
        )}
      </div>

      {/* Active Status */}
      <div className="space-y-2">
        <Label>Status</Label>
        <div className="flex gap-2">
          <Button
            type="button"
            variant={isActive ? "default" : "outline"}
            size="sm"
            onClick={() => setIsActive(true)}
            disabled={isLoading}
          >
            Active
          </Button>
          <Button
            type="button"
            variant={!isActive ? "default" : "outline"}
            size="sm"
            onClick={() => setIsActive(false)}
            disabled={isLoading}
          >
            Inactive
          </Button>
        </div>
      </div>

      {/* Actions */}
      <div className="flex justify-end gap-2 pt-2">
        <Button
          type="button"
          variant="outline"
          onClick={onCancel}
          disabled={isLoading}
        >
          Cancel
        </Button>
        <Button type="submit" disabled={isLoading} className="cursor-pointer">
          {isLoading
            ? isEdit
              ? "Saving..."
              : "Creating..."
            : isEdit
              ? "Save Changes"
              : "Create Product"}
        </Button>
      </div>
    </form>
  );
}
