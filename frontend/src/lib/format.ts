/** Formatos de República Dominicana: punto decimal y coma de miles ("1,250.5"). */
const LOCALE = "es-DO";

const kva = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 1 });
const percent = new Intl.NumberFormat(LOCALE, { minimumFractionDigits: 1, maximumFractionDigits: 1 });
const money = new Intl.NumberFormat(LOCALE, { style: "currency", currency: "DOP", maximumFractionDigits: 0 });
const date = new Intl.DateTimeFormat(LOCALE, { day: "numeric", month: "short", year: "numeric" });
// Hora de República Dominicana aunque el servidor esté en otra zona (Azure usa UTC).
const dateTime = new Intl.DateTimeFormat(LOCALE, {
  day: "numeric",
  month: "short",
  year: "numeric",
  hour: "numeric",
  minute: "2-digit",
  timeZone: "America/Santo_Domingo",
});
const bytes = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 1 });
const relative = new Intl.RelativeTimeFormat(LOCALE, { numeric: "auto" });

const DAY_MS = 24 * 60 * 60 * 1000;

export function formatKva(value: number): string {
  return `${kva.format(value)} kVA`;
}

/** El porcentaje tal como llega de la API: puede ser negativo en un transformador saturado. */
export function formatPercent(value: number): string {
  return `${percent.format(value)} %`;
}

export function formatMoney(value: number): string {
  return money.format(value);
}

export function formatDate(value: Date | string): string {
  return date.format(new Date(value));
}

export function formatDateTime(value: Date | string): string {
  return dateTime.format(new Date(value));
}

/** Tamaño de archivo: "850 KB", "2.4 MB". */
export function formatFileSize(size: number): string {
  if (size < 1024 * 1024) return `${bytes.format(Math.max(1, Math.round(size / 1024)))} KB`;
  return `${bytes.format(size / (1024 * 1024))} MB`;
}

/** Días completos transcurridos entre dos fechas (0 si la fecha es futura). */
export function daysBetween(from: Date | string, to: Date): number {
  return Math.max(0, Math.floor((to.getTime() - new Date(from).getTime()) / DAY_MS));
}

/** "hoy", "ayer", "hace 12 días". */
export function formatDaysAgo(value: Date | string, now: Date): string {
  return relative.format(-daysBetween(value, now), "day");
}
