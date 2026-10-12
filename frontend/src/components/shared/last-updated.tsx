import { Clock, TriangleAlert } from "lucide-react";

import { daysBetween, formatDate, formatDaysAgo } from "@/lib/format";
import { cn } from "@/lib/utils";

/** Días a partir de los cuales el dato se marca como desactualizado. Provisional: decisión D-10 con Edesur. */
export const STALE_AFTER_DAYS = 30;

type Props = {
  /** Fecha de la última actualización informada por la distribuidora (RD-02). */
  date: string | Date | null | undefined;
  now?: Date;
  staleAfterDays?: number;
  className?: string;
};

export function LastUpdated({ date, now = new Date(), staleAfterDays = STALE_AFTER_DAYS, className }: Props) {
  if (!date) {
    return <p className={cn("text-sm text-muted-foreground", className)}>Sin fecha de actualización</p>;
  }

  const stale = daysBetween(date, now) > staleAfterDays;
  const Icon = stale ? TriangleAlert : Clock;

  return (
    <p className={cn("flex items-center gap-1.5 text-sm", stale ? "text-status-conditioned-text" : "text-muted-foreground", className)}>
      <Icon aria-hidden="true" className="size-4 shrink-0" />
      <span>
        Actualizado el <time dateTime={new Date(date).toISOString()}>{formatDate(date)}</time> ({formatDaysAgo(date, now)})
        {stale && <>. Dato con más de {staleAfterDays} días</>}
      </span>
    </p>
  );
}
