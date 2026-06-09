"use client";

import { useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { Input } from "@/components/ui/input";

interface PasswordInputProps extends React.ComponentProps<typeof Input> {
  onToggleShow?: (show: boolean) => void;
}

export function PasswordInput({ onToggleShow, ...props }: PasswordInputProps) {
  const [show, setShow] = useState(false);

  function handleToggle() {
    const next = !show;
    setShow(next);
    onToggleShow?.(next);
  }

  return (
    <div className="relative">
      <Input type={show ? "text" : "password"} {...props} />
      <button
        type="button"
        onClick={handleToggle}
        className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
        tabIndex={-1}
      >
        {show ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
      </button>
    </div>
  );
}
