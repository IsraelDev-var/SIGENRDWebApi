import { publicEnv, type PublicEnv } from "@/config/env";

/** Par [longitud, latitud] en WGS84 (RN-04), el orden que usa MapLibre. */
export type LngLat = [longitude: number, latitude: number];

/** Extremos de República Dominicana: suroeste y noreste. */
export const DOMINICAN_REPUBLIC_BOUNDS: [southWest: LngLat, northEast: LngLat] = [
  [-72.01, 17.47],
  [-68.32, 19.93],
];

/**
 * Margen alrededor del país hasta donde se puede mover el mapa. MapLibre no deja que la vista salga de estos
 * límites, así que deben caber en la vista inicial de un móvil vertical: si son más bajos que la franja visible,
 * MapLibre acerca el mapa y corta el este y el oeste del país. Por eso el margen vertical es mayor.
 */
const MARGIN_LNG_DEGREES = 2;
const MARGIN_LAT_DEGREES = 3.5;

export const MAP_MAX_BOUNDS: [LngLat, LngLat] = [
  [DOMINICAN_REPUBLIC_BOUNDS[0][0] - MARGIN_LNG_DEGREES, DOMINICAN_REPUBLIC_BOUNDS[0][1] - MARGIN_LAT_DEGREES],
  [DOMINICAN_REPUBLIC_BOUNDS[1][0] + MARGIN_LNG_DEGREES, DOMINICAN_REPUBLIC_BOUNDS[1][1] + MARGIN_LAT_DEGREES],
];

/**
 * Vista inicial: el país completo en cualquier ancho. Se encuadran los límites en lugar de fijar un zoom,
 * que en un móvil de 360 px dejaba fuera el este y el oeste.
 */
export const INITIAL_FIT_PADDING_PX = 16;

// Encuadrar el país a 360 px de ancho necesita un zoom cercano a 5.9.
export const MAP_ZOOM_LIMITS = { min: 5, max: 18 };

/**
 * Fuente para los números de los grupos: debe existir en las fuentes (glyphs) del estilo base.
 * CARTO ofrece Montserrat y Open Sans; si cambia el proveedor (D-2), revisar esta lista.
 */
export const MAP_LABEL_FONT = ["Montserrat Medium", "Open Sans Bold", "Noto Sans Regular"];

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
