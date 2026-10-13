import { describe, expect, it } from "vitest";

import { publicEnvSchema } from "@/config/env";
import { DOMINICAN_REPUBLIC_BOUNDS, INITIAL_FIT_PADDING_PX, MAP_MAX_BOUNDS, MAP_ZOOM_LIMITS, resolveMapStyles } from "@/config/map";

describe("vista del mapa", () => {
  it("encuadra un área válida de República Dominicana (suroeste antes que noreste)", () => {
    const [[west, south], [east, north]] = DOMINICAN_REPUBLIC_BOUNDS;

    expect(west).toBeLessThan(east);
    expect(south).toBeLessThan(north);
    // Cabo Engaño (este) y Pedernales (oeste) quedan dentro.
    expect(west).toBeLessThan(-71.7);
    expect(east).toBeGreaterThan(-68.33);
  });

  it("deja mover el mapa más allá del país sin perderlo", () => {
    const [[west, south], [east, north]] = DOMINICAN_REPUBLIC_BOUNDS;
    const [[maxWest, maxSouth], [maxEast, maxNorth]] = MAP_MAX_BOUNDS;

    expect(maxWest).toBeLessThan(west);
    expect(maxSouth).toBeLessThan(south);
    expect(maxEast).toBeGreaterThan(east);
    expect(maxNorth).toBeGreaterThan(north);
  });
});

describe("vista inicial en un móvil vertical (CA-13)", () => {
  // Pantalla de 360 × 800 px; el mapa ocupa casi todo el alto.
  const width = 360;
  const mapHeight = 720;
  const [[west, south], [east, north]] = DOMINICAN_REPUBLIC_BOUNDS;
  const centerLatRad = (((south + north) / 2) * Math.PI) / 180;

  // Grados de longitud por píxel al encuadrar el país de este a oeste (Web Mercator, teselas de 512 px).
  const lngPerPx = (east - west) / (width - 2 * INITIAL_FIT_PADDING_PX);
  const fitZoom = Math.log2(360 / (512 * lngPerPx));

  it("el zoom necesario está permitido", () => {
    expect(fitZoom).toBeGreaterThanOrEqual(MAP_ZOOM_LIMITS.min);
  });

  it("los límites de movimiento son más altos que la franja visible, así MapLibre no acerca el mapa", () => {
    const visibleLatSpan = mapHeight * lngPerPx * Math.cos(centerLatRad);
    const maxBoundsLatSpan = MAP_MAX_BOUNDS[1][1] - MAP_MAX_BOUNDS[0][1];

    expect(maxBoundsLatSpan).toBeGreaterThan(visibleLatSpan);
  });
});

describe("estilos del mapa base", () => {
  it("usa los configurados", () => {
    const styles = resolveMapStyles({
      NEXT_PUBLIC_MAP_STYLE_LIGHT: "https://tiles.example.org/light.json",
      NEXT_PUBLIC_MAP_STYLE_DARK: "https://tiles.example.org/dark.json",
    });

    expect(styles).toEqual({
      light: "https://tiles.example.org/light.json",
      dark: "https://tiles.example.org/dark.json",
    });
  });

  it("vuelve a CARTO si no hay configuración", () => {
    const styles = resolveMapStyles(publicEnvSchema.parse({}));

    expect(styles.light).toContain("basemaps.cartocdn.com");
    expect(styles.dark).toContain("basemaps.cartocdn.com");
  });

  it("trata una variable vacía como no definida", () => {
    expect(publicEnvSchema.parse({ NEXT_PUBLIC_MAP_STYLE_LIGHT: "" }).NEXT_PUBLIC_MAP_STYLE_LIGHT).toBeUndefined();
  });

  it("rechaza una URL inválida al arrancar", () => {
    expect(() => publicEnvSchema.parse({ NEXT_PUBLIC_MAP_STYLE_DARK: "no-es-una-url" })).toThrow();
  });
});
