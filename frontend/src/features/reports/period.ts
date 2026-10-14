// Período de los reportes: fechas de la URL, límites de la API (hasta 3 años) y el "hasta" exclusivo que espera.
// Sin dependencias del servidor para poder probarlo.

const DAY_MS = 24 * 60 * 60 * 1000;
const MAX_DAYS = 3 * 366;
const isDate = (value: string | undefined) => !!value && /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(Date.parse(value));

export type Period = { from: string; to: string };

/**
 * Período del reporte desde la URL (?desde=&hasta=, ambos incluidos). Sin fechas válidas: los últimos 12 meses,
 * igual que la API. Devuelve también el error si el rango no sirve (al revés o de más de 3 años).
 */
export function periodFrom(desde: string | undefined, hasta: string | undefined, today = new Date()): { period: Period; error: string | null } {
  const end = isDate(hasta) ? hasta! : today.toISOString().slice(0, 10);
  const defaultStart = new Date(Date.parse(end) - 365 * DAY_MS).toISOString().slice(0, 10);
  const start = isDate(desde) ? desde! : defaultStart;
  const days = (Date.parse(end) - Date.parse(start)) / DAY_MS;
  const error = days < 0 ? "La fecha inicial debe ser anterior a la final." : days > MAX_DAYS ? "El período no puede superar los 3 años." : null;
  return { period: { from: start, to: end }, error };
}

/** La API toma "To" como límite exclusivo: para incluir el último día se pide hasta el día siguiente. */
export function periodQuery({ from, to }: Period): URLSearchParams {
  const next = new Date(Date.parse(to) + DAY_MS).toISOString().slice(0, 10);
  return new URLSearchParams({ From: from, To: next });
}
