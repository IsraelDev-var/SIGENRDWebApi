import { formatKva } from "@/lib/format";
import { cn } from "@/lib/utils";

type Props = {
  value: number | null | undefined;
  className?: string;
};

/** Capacidad en kVA con cifras tabulares, para que las columnas se alineen. */
export function KvaValue({ value, className }: Props) {
  return (
    <span className={cn("font-mono tabular-nums", className)}>
      {typeof value === "number" && Number.isFinite(value) ? formatKva(value) : "Sin dato"}
    </span>
  );
}
