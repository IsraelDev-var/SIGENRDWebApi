import { publicEnv, type PublicEnv } from "@/config/env";

/** Par [longitud, latitud] en WGS84 (RN-04), el orden que usa MapLibre. */
export type LngLat = [longitude: number, latitude: number];

/** Extremos de República Dominicana: suroeste y noreste. */
export const DOMINICAN_REPUBLIC_BOUNDS: [southWest: LngLat, northEast: LngLat] = [
  [-72.01, 17.47],
  [-68.32, 19.93],
];

/** Margen para que el usuario pueda mover el mapa un poco más allá de la costa. */
const BOUNDS_MARGIN_DEGREES = 1.5;

export const MAP_MAX_BOUNDS: [LngLat, LngLat] = [
  [DOMINICAN_REPUBLIC_BOUNDS[0][0] - BOUNDS_MARGIN_DEGREES, DOMINICAN_REPUBLIC_BOUNDS[0][1] - BOUNDS_MARGIN_DEGREES],
  [DOMINICAN_REPUBLIC_BOUNDS[1][0] + BOUNDS_MARGIN_DEGREES, DOMINICAN_REPUBLIC_BOUNDS[1][1] + BOUNDS_MARGIN_DEGREES],
];

/** Vista inicial: el país completo. */
export const INITIAL_VIEW = {
  center: [-70.16, 18.74] as LngLat,
  zoom: 7,
};

export const MAP_ZOOM_LIMITS = { min: 6, max: 18 };

const CARTO_STYLES = {
  light: "https://basemaps.cartocdn.com/gl/positron-gl-style/style.json",
  dark: "https://basemaps.cartocdn.com/gl/dark-matter-gl-style/style.json",
};

/** Estilos del mapa base: los configurados o, si faltan, CARTO (solo desarrollo, decisión D-2). */
export function resolveMapStyles(env: PublicEnv): { light: string; dark: string } {
  return {
    light: env.NEXT_PUBLIC_MAP_STYLE_LIGHT ?? CARTO_STYLES.light,
    dark: env.NEXT_PUBLIC_MAP_STYLE_DARK ?? CARTO_STYLES.dark,
  };
}

export const MAP_STYLES = resolveMapStyles(publicEnv);
