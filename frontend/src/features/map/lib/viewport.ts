/**
 * Cálculos del área visible del mapa. Funciones puras: se prueban sin navegador.
 */

export type Bounds = { minLatitude: number; minLongitude: number; maxLatitude: number; maxLongitude: number };

/** Desde este zoom se piden los transformadores uno a uno; por debajo, los grupos del servidor (B-2). */
export const POINTS_MIN_ZOOM = 12;

/** Margen alrededor del área visible: al mover un poco el mapa no hace falta pedir de nuevo. */
const PADDING_RATIO = 0.2;
/** Paso de redondeo en grados (~1 km): áreas casi iguales comparten la misma petición y la misma caché. */
const STEP = 0.01;

const floorTo = (value: number) => Math.floor(value / STEP) * STEP;
const ceilTo = (value: number) => Math.ceil(value / STEP) * STEP;
const round = (value: number) => Number(value.toFixed(4));

/** Área a pedir: la visible con margen, redondeada hacia afuera y dentro de los límites válidos. */
export function requestBounds(visible: Bounds): Bounds {
  const latPad = (visible.maxLatitude - visible.minLatitude) * PADDING_RATIO;
  const lngPad = (visible.maxLongitude - visible.minLongitude) * PADDING_RATIO;

  return {
    minLatitude: round(Math.max(-90, floorTo(visible.minLatitude - latPad))),
    minLongitude: round(Math.max(-180, floorTo(visible.minLongitude - lngPad))),
    maxLatitude: round(Math.min(90, ceilTo(visible.maxLatitude + latPad))),
    maxLongitude: round(Math.min(180, ceilTo(visible.maxLongitude + lngPad))),
  };
}

export function contains(outer: Bounds, inner: Bounds): boolean {
  return (
    outer.minLatitude <= inner.minLatitude &&
    outer.minLongitude <= inner.minLongitude &&
    outer.maxLatitude >= inner.maxLatitude &&
    outer.maxLongitude >= inner.maxLongitude
  );
}

export type DataMode = "clusters" | "points";

export function dataModeFor(zoom: number): DataMode {
  return zoom >= POINTS_MIN_ZOOM ? "points" : "clusters";
}

/** Zoom entero para pedir grupos: el servidor agrupa igual dentro del mismo nivel. */
export function clusterZoom(zoom: number): number {
  return Math.max(0, Math.min(22, Math.floor(zoom)));
}

/**
 * Coordenadas escritas por la persona: "18.47, -69.90", "18.47 -69.90" o "18,47; -69,90".
 * Devuelve null si no son coordenadas válidas (entonces se busca como código).
 */
export function parseCoordinates(text: string): { latitude: number; longitude: number } | null {
  const trimmed = text.trim();
  // Con punto y coma o espacio entre ambos números, la coma puede ser decimal.
  const parts = trimmed.includes(";")
    ? trimmed.split(";").map((p) => p.trim().replace(",", "."))
    : trimmed.split(/[\s,]+/).filter(Boolean);

  if (parts.length !== 2) return null;
  const [latitude, longitude] = parts.map(Number) as [number, number];
  if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) return null;
  if (Math.abs(latitude) > 90 || Math.abs(longitude) > 180) return null;
  return { latitude, longitude };
}
