import type { ComponentProps, ReactNode } from "react";

import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

import type { Option } from "../labels";

/** Selectores, textos largos y opciones del asistente, con etiqueta, ayuda y error asociados (WCAG 1.3.1, 3.3.1). */

export const controlClass =
  "w-full rounded-lg border border-input bg-transparent px-2.5 py-1.5 text-base outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30";

function describedBy(id: string, description?: ReactNode, errors?: string[]) {
  return [description ? `${id}-ayuda` : null, errors?.length ? `${id}-error` : null].filter(Boolean).join(" ") || undefined;
}

function Help({ id, description, errors }: { id: string; description?: ReactNode; errors?: string[] }) {
  return (
    <>
      {description && (
        <p id={`${id}-ayuda`} className="text-sm text-muted-foreground">
          {description}
        </p>
      )}
      {errors?.length ? (
        <p id={`${id}-error`} className="text-sm text-destructive">
          {errors.join(" ")}
        </p>
      ) : null}
    </>
  );
}

type SelectProps<T extends string> = Omit<ComponentProps<"select">, "id" | "children"> & {
  id: string;
  label: string;
  options: Option<T>[];
  description?: ReactNode;
  errors?: string[];
};

export function SelectField<T extends string>({ id, label, options, description, errors, className, ...select }: SelectProps<T>) {
  return (
    <div className={cn("grid gap-2", className)}>
      <Label htmlFor={id}>{label}</Label>
      <select
        id={id}
        aria-invalid={errors?.length ? true : undefined}
        aria-describedby={describedBy(id, description, errors)}
        className={cn(controlClass, "h-8 py-1")}
        {...select}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      <Help id={id} description={description} errors={errors} />
    </div>
  );
}

type TextareaProps = Omit<ComponentProps<"textarea">, "id"> & {
  id: string;
  label: string;
  description?: ReactNode;
  errors?: string[];
};

export function TextareaField({ id, label, description, errors, className, ...textarea }: TextareaProps) {
  return (
    <div className={cn("grid gap-2", className)}>
      <Label htmlFor={id}>{label}</Label>
      <textarea
        id={id}
        rows={3}
        aria-invalid={errors?.length ? true : undefined}
        aria-describedby={describedBy(id, description, errors)}
        className={controlClass}
        {...textarea}
      />
      <Help id={id} description={description} errors={errors} />
    </div>
  );
}

type ChoiceProps<T extends string> = {
  name: string;
  legend: string;
  options: Option<T>[];
  value: string;
  onChange: (value: T) => void;
  errors?: string[];
};

/** Opciones excluyentes como tarjetas: más fáciles de tocar en el teléfono que un selector. */
export function ChoiceCards<T extends string>({ name, legend, options, value, onChange, errors }: ChoiceProps<T>) {
  const errorId = errors?.length ? `${name}-error` : undefined;
  return (
    <fieldset className="grid gap-2" aria-describedby={errorId}>
      <legend className="mb-2 text-sm font-medium">{legend}</legend>
      <div className="grid gap-2 sm:grid-cols-2">
        {options.map((option) => (
          <label
            key={option.value}
            className="flex cursor-pointer items-start gap-3 rounded-lg border p-3 has-checked:border-primary has-checked:bg-primary/5 has-focus-visible:ring-3 has-focus-visible:ring-ring/50"
          >
            <input
              type="radio"
              name={name}
              value={option.value}
              checked={value === option.value}
              onChange={() => onChange(option.value)}
              className="mt-1 size-4 accent-primary"
            />
            <span className="grid gap-0.5">
              <span className="font-medium">{option.label}</span>
              {option.hint && <span className="text-sm text-muted-foreground">{option.hint}</span>}
            </span>
          </label>
        ))}
      </div>
      {errorId && (
        <p id={errorId} className="text-sm text-destructive">
          {errors!.join(" ")}
        </p>
      )}
    </fieldset>
  );
}
