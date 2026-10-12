import { asTransformerStatus, TRANSFORMER_STATUS } from "@/config/status";
import { formatPercent } from "@/lib/format";
import { cn } from "@/lib/utils";

type Props = {
  /** Porcentaje calculado por la API (RN-01). Puede ser negativo o nulo si falta el dato. */
  availabilityPct: number | null | undefined;
  /** Estado calculado por la API: decide el color, no el porcentaje. */
  status: string;
  label?: string;
  className?: string;
};

/**
 * Barra de capacidad disponible. Muestra el valor de la API sin recalcularlo; la barra solo se recorta a 0–100.
 * En Mantenimiento la disponibilidad no se calcula (RN-02), así que no se dibuja la barra.
 */
export function CapacityMeter({ availabilityPct, status, label = "Capacidad disponible", className }: Props) {
  const known = asTransformerStatus(status);
  const hasValue = typeof availabilityPct === "number" && Number.isFinite(availabilityPct);

  if (known === "Maintenance" || !hasValue) {
    return (
      <div className={cn("text-sm text-muted-foreground", className)}>
        {label}: {known === "Maintenance" ? "no se calcula durante el mantenimiento" : "sin dato"}
      </div>
    );
  }

  const width = Math.min(100, Math.max(0, availabilityPct));
  const fill = known ? TRANSFORMER_STATUS[known].fillClass : "bg-muted-foreground";

  return (
    <div className={cn("grid gap-1", className)}>
      <div className="flex items-baseline justify-between gap-2 text-sm">
        <span className="text-muted-foreground">{label}</span>
        <span className="font-mono font-medium tabular-nums">{formatPercent(availabilityPct)}</span>
      </div>
      <div
        role="meter"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={width}
        aria-valuetext={formatPercent(availabilityPct)}
        className="h-2 overflow-hidden rounded-full bg-muted"
      >
        <div className={cn("h-full rounded-full", fill)} style={{ width: `${width}%` }} />
      </div>
    </div>
  );
}
