import { TRANSFORMER_STATUS, TRANSFORMER_STATUSES, type TransformerStatus } from "@/config/status";
import { cn } from "@/lib/utils";

/** El símbolo de cada estado tal como se dibuja en el mapa (transformer-layers.tsx). */
function Marker({ status }: { status: TransformerStatus }) {
  if (status === "Critical") return <span className="size-3.5 rounded-full border-[3px] border-status-critical bg-card" />;
  if (status === "Saturated")
    return (
      <span className="flex size-3.5 items-center justify-center rounded-full bg-status-saturated text-[11px] leading-none font-bold text-card">
        ×
      </span>
    );
  return <span className={cn("size-3.5 rounded-full border border-card", TRANSFORMER_STATUS[status].fillClass)} />;
}

/** Leyenda del mapa (RN-02): símbolo, nombre y qué significa para pedir una conexión. */
export function MapLegend({ className }: { className?: string }) {
  return (
    <section aria-label="Leyenda del mapa" className={cn("text-xs", className)}>
      <ul className="grid gap-1.5">
        {TRANSFORMER_STATUSES.map((status) => (
          <li key={status} className="flex items-center gap-2" title={TRANSFORMER_STATUS[status].description}>
            <span aria-hidden="true" className="flex w-4 justify-center">
              <Marker status={status} />
            </span>
            {TRANSFORMER_STATUS[status].label}
          </li>
        ))}
        <li className="flex items-center gap-2">
          <span aria-hidden="true" className="flex w-4 justify-center">
            <span className="size-4 rounded-full border-2 border-status-critical bg-primary" />
          </span>
          Grupo; anillo rojo si incluye Crítica o Saturada
        </li>
      </ul>
    </section>
  );
}
