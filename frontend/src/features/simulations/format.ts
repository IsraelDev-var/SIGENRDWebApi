import { formatPercent } from "@/lib/format";

const years = new Intl.NumberFormat("es-DO", { minimumFractionDigits: 1, maximumFractionDigits: 1 });
const kwh = new Intl.NumberFormat("es-DO", { maximumFractionDigits: 0 });
const kw = new Intl.NumberFormat("es-DO", { maximumFractionDigits: 2 });

/** Retorno simple: sin ahorro no hay retorno (la API devuelve null). */
export function formatPayback(value: number | null | undefined): string {
  return value == null ? "Sin retorno" : `${years.format(value)} años`;
}

export function formatKwh(value: number): string {
  return `${kwh.format(value)} kWh`;
}

export function formatKw(value: number): string {
  return `${kw.format(value)} kWp`;
}

export function formatRoi(value: number): string {
  return formatPercent(value);
}

export function formatDistance(meters: number): string {
  return meters < 1000 ? `${Math.round(meters)} m` : `${years.format(meters / 1000)} km`;
}
