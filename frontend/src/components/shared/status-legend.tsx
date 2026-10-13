import { TRANSFORMER_STATUS, type TransformerStatus } from "@/config/status";
import { cn } from "@/lib/utils";

const ORDER: TransformerStatus[] = ["Available", "Conditioned", "Critical", "Saturated", "Maintenance"];

type Props = {
  /** Con descripción, para la página de requisitos; sin ella, compacta para el mapa. */
  detailed?: boolean;
  className?: string;
};

/** Leyenda del semáforo de disponibilidad (RN-02). */
export function StatusLegend({ detailed = false, className }: Props) {
  return (
    <section aria-label="Leyenda de disponibilidad" className={cn("text-sm", className)}>
      <ul className={cn("grid gap-2", detailed ? "" : "grid-cols-1")}>
        {ORDER.map((status) => {
          const { label, description, icon: Icon, fillClass } = TRANSFORMER_STATUS[status];
          return (
            <li key={status} className="flex items-start gap-2">
              <span className={cn("mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-full text-background", fillClass)}>
                <Icon aria-hidden="true" className="size-3.5" />
              </span>
              <span>
                <span className="font-medium">{label}</span>
                {detailed && <span className="block text-muted-foreground">{description}</span>}
              </span>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
