import { formatPercent } from "@/lib/format";
import { cn } from "@/lib/utils";

/**
 * Disponibilidad de un transformador en listas y búsquedas. En Mantenimiento no se calcula (RN-02):
 * se dice eso en lugar de mostrar el número que tenga guardado.
 */
export function AvailabilityValue({ pct, status, className }: { pct: number; status: string; className?: string }) {
  return (
    <span className={cn("font-mono tabular-nums", className)}>
      {status === "Maintenance" ? "No se calcula" : formatPercent(pct)}
    </span>
  );
}
