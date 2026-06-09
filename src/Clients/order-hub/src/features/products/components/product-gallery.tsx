"use client";

import { useState } from "react";
import { Package } from "lucide-react";

interface ProductGalleryProps {
  mainImageUrl: string;
  galleryImageUrls: string[];
}

export function ProductGallery({
  mainImageUrl,
  galleryImageUrls,
}: ProductGalleryProps) {
  const allImages = [
    mainImageUrl,
    ...galleryImageUrls,
  ].filter(Boolean);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const selectedImage = allImages[selectedIndex] ?? "";

  return (
    <div className="space-y-3">
      {/* Main image */}
      <div className="aspect-square w-full overflow-hidden rounded-lg bg-muted">
        {selectedImage ? (
          <img
            src={selectedImage}
            alt="Product image"
            className="h-full w-full object-cover"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center">
            <Package className="size-24 text-muted-foreground/50" />
          </div>
        )}
      </div>

      {/* Thumbnails */}
      {allImages.length > 1 && (
        <div className="flex gap-2">
          {allImages.map((img, index) => (
            <button
              key={index}
              onClick={() => setSelectedIndex(index)}
              className={`h-16 w-16 overflow-hidden rounded-md border-2 transition-colors ${
                index === selectedIndex
                  ? "border-primary"
                  : "border-transparent hover:border-muted-foreground/30"
              }`}
            >
              <img
                src={img}
                alt={`Thumbnail ${index + 1}`}
                className="h-full w-full object-cover"
              />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
