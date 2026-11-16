"use client";

import { Eye, EyeOff } from "lucide-react";
import { useState, type ComponentProps } from "react";

import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

/** Contraseña con un botón para verla mientras se escribe. El botón es type="button": no envía el formulario. */
export function PasswordInput({ id, className, ...input }: Omit<ComponentProps<typeof Input>, "type">) {
  const [visible, setVisible] = useState(false);
  const Icon = visible ? EyeOff : Eye;

  return (
    <div className="relative">
      <Input id={id} type={visible ? "text" : "password"} className={cn("pr-9", className)} {...input} />
      <button
        type="button"
        aria-controls={id}
        aria-pressed={visible}
        aria-label={visible ? "Ocultar contraseña" : "Mostrar contraseña"}
        onClick={() => setVisible((v) => !v)}
        className="absolute inset-y-0 right-0 flex w-9 items-center justify-center rounded-r-lg text-muted-foreground outline-none hover:text-foreground focus-visible:ring-3 focus-visible:ring-ring/50"
      >
        <Icon aria-hidden="true" className="size-4" />
      </button>
    </div>
  );
}
