import { CircleAlert, CircleCheck } from "lucide-react";
import type { ComponentProps, ReactNode } from "react";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

import { PasswordInput } from "./password-input";

type FieldProps = Omit<ComponentProps<typeof Input>, "id" | "name"> & {
  name: string;
  label: string;
  /** Texto de ayuda permanente, por ejemplo las reglas de la contraseña. */
  description?: ReactNode;
  errors?: string[];
  /** Prefijo para el id, cuando hay varios formularios en la misma página. */
  idPrefix?: string;
};

/** Campo con etiqueta, ayuda y error asociados (WCAG 1.3.1, 3.3.1, 3.3.2). Las contraseñas llevan el botón para verlas. */
export function FormField({ name, label, description, errors, idPrefix = "campo", className, type, ...input }: FieldProps) {
  const id = `${idPrefix}-${name}`;
  const descriptionId = description ? `${id}-ayuda` : undefined;
  const errorId = errors?.length ? `${id}-error` : undefined;
  const describedBy = [descriptionId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className={cn("grid gap-2", className)}>
      {/* "required" ya lo anuncian los lectores de pantalla: no se repite en la etiqueta. */}
      <Label htmlFor={id}>{label}</Label>
      {type === "password" ? (
        <PasswordInput id={id} name={name} aria-invalid={errorId ? true : undefined} aria-describedby={describedBy} {...input} />
      ) : (
        <Input id={id} name={name} type={type} aria-invalid={errorId ? true : undefined} aria-describedby={describedBy} {...input} />
      )}
      {description && (
        <p id={descriptionId} className="text-sm text-muted-foreground">
          {description}
        </p>
      )}
      {errorId && (
        <p id={errorId} className="text-sm text-destructive">
          {errors!.join(" ")}
        </p>
      )}
    </div>
  );
}

/** Resultado del envío: error general o confirmación. Se anuncia a los lectores de pantalla. */
export function FormMessage({ status, message }: { status: "idle" | "error" | "success"; message?: string }) {
  if (status === "idle" || !message) return null;
  const success = status === "success";
  const Icon = success ? CircleCheck : CircleAlert;

  return (
    <div
      role={success ? "status" : "alert"}
      className={cn(
        "flex items-start gap-2 rounded-md px-3 py-2 text-sm",
        success ? "bg-tone-success-soft text-tone-success-text" : "bg-tone-danger-soft text-tone-danger-text",
      )}
    >
      <Icon aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
      <p>{message}</p>
    </div>
  );
}
