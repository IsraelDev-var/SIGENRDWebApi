import type { components } from "@/lib/api/schema";

import type { MonthlyPoint } from "./components/report-charts";

const monthLabel = new Intl.DateTimeFormat("es-DO", { month: "short", year: "numeric", timeZone: "UTC" });

/** "2026-10" → "oct 2026". Un valor que no tiene esa forma se muestra tal cual. */
export function formatMonth(key: string): string {
  return /^\d{4}-\d{2}$/.test(key) ? monthLabel.format(new Date(`${key}-01T00:00:00Z`)) : key;
}

/** Barras apiladas por mes: aprobadas, rechazadas y el resto (en curso o canceladas), sin contar dos veces. */
export function monthlyPoints(items: components["schemas"]["MonthItem"][]): MonthlyPoint[] {
  return items.map((m) => ({
    month: m.month,
    label: formatMonth(m.month),
    approved: m.approved,
    rejected: m.rejected,
    others: Math.max(0, m.total - m.approved - m.rejected),
  }));
}

/** Días con un decimal, o "Sin dato" si todavía no hay solicitudes resueltas. */
export function formatDays(value: number | null | undefined): string {
  return value == null ? "Sin dato" : `${value.toLocaleString("es-DO", { maximumFractionDigits: 1 })} días`;
}
