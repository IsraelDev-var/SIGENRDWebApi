import { describe, expect, it } from "vitest";

import { publicEnvSchema } from "@/config/env";
import { DOMINICAN_REPUBLIC_BOUNDS, INITIAL_VIEW, MAP_MAX_BOUNDS, resolveMapStyles } from "@/config/map";

describe("vista del mapa", () => {
  it("empieza dentro de República Dominicana", () => {
    const [[west, south], [east, north]] = DOMINICAN_REPUBLIC_BOUNDS;
    const [lng, lat] = INITIAL_VIEW.center;

    expect(lng).toBeGreaterThan(west);
    expect(lng).toBeLessThan(east);
    expect(lat).toBeGreaterThan(south);
    expect(lat).toBeLessThan(north);
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
