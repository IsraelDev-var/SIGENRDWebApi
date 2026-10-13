import { readFileSync } from "node:fs";
import { join } from "node:path";

import { describe, expect, it } from "vitest";

/**
 * Contraste WCAG 2.1 AA de los tokens reales de globals.css, en los dos temas (RNF-15):
 * 4.5:1 para texto (1.4.3) y 3:1 para bordes de controles, foco y marcadores (1.4.11).
 */

const css = readFileSync(join(process.cwd(), "src/app/globals.css"), "utf8");

function readTokens(block: string): Record<string, string> {
  const tokens: Record<string, string> = {};
  for (const match of block.matchAll(/--([\w-]+):\s*(#[0-9a-f]{6})\s*;/gi)) {
    tokens[match[1]!] = match[2]!.toLowerCase();
  }
  return tokens;
}

function blockAfter(marker: string): string {
  const start = css.indexOf(marker);
  if (start < 0) throw new Error(`No se encontró "${marker}" en globals.css`);
  const open = css.indexOf("{", start + marker.length - 1);
  let depth = 0;
  for (let i = open; i < css.length; i += 1) {
    if (css[i] === "{") depth += 1;
    if (css[i] === "}") depth -= 1;
    if (depth === 0) return css.slice(open, i + 1);
  }
  throw new Error(`Bloque sin cerrar tras "${marker}"`);
}

const light = readTokens(blockAfter("\n:root {"));
const darkOverrides = readTokens(blockAfter("@media (prefers-color-scheme: dark) {"));
const dark = { ...light, ...darkOverrides };

function luminance(hex: string): number {
  const channels = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255);
  const [r, g, b] = channels.map((c) => (c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4)) as [number, number, number];
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

export function contrast(a: string, b: string): number {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x) as [number, number];
  return (hi + 0.05) / (lo + 0.05);
}

const statuses = ["available", "conditioned", "critical", "saturated", "maintenance"];
const tones = ["info", "review", "warning", "success", "danger", "neutral"];

// [texto, fondo]
const textPairs: [string, string][] = [
  ["foreground", "background"],
  ["foreground", "card"],
  ["card-foreground", "card"],
  ["popover-foreground", "popover"],
  ["muted-foreground", "background"],
  ["muted-foreground", "card"],
  ["muted-foreground", "muted"],
  ["primary-foreground", "primary"],
  ["secondary-foreground", "secondary"],
  ["accent-foreground", "accent"],
  ["link", "background"],
  ["link", "card"],
  ["destructive", "card"],
  ["sidebar-foreground", "sidebar"],
  ["sidebar-primary-foreground", "sidebar-primary"],
  ["sidebar-accent-foreground", "sidebar-accent"],
  ...statuses.map((s): [string, string] => [`status-${s}-text`, `status-${s}-soft`]),
  ...tones.map((t): [string, string] => [`tone-${t}-text`, `tone-${t}-soft`]),
];

// [elemento gráfico, fondo]
const graphicPairs: [string, string][] = [
  ["input", "card"],
  ["input", "background"],
  ["ring", "card"],
  ["ring", "background"],
  ["primary", "background"],
  ...statuses.flatMap((s): [string, string][] => [
    [`status-${s}`, "card"],
    [`status-${s}`, "background"],
  ]),
];

describe.each([
  ["claro", light],
  ["oscuro", dark],
])("tema %s", (_name, tokens) => {
  it.each(textPairs)("texto %s sobre %s ≥ 4.5:1", (fg, bg) => {
    expect(tokens[fg], `falta --${fg}`).toBeDefined();
    expect(tokens[bg], `falta --${bg}`).toBeDefined();
    expect(contrast(tokens[fg]!, tokens[bg]!)).toBeGreaterThanOrEqual(4.5);
  });

  it.each(graphicPairs)("elemento %s sobre %s ≥ 3:1", (fg, bg) => {
    expect(contrast(tokens[fg]!, tokens[bg]!)).toBeGreaterThanOrEqual(3);
  });
});

describe("lectura de globals.css", () => {
  it("encuentra los dos temas completos", () => {
    // Sin esto, un error al leer el bloque oscuro haría que el tema oscuro "pase" con los valores claros.
    expect(Object.keys(light).length).toBeGreaterThan(50);
    // El tema oscuro redefine todos los colores: ninguno se hereda del claro por olvido.
    expect(Object.keys(darkOverrides).sort()).toEqual(Object.keys(light).sort());
    expect(darkOverrides.background).not.toBe(light.background);
  });
});

describe("cálculo de contraste", () => {
  it("coincide con los valores de referencia de WCAG", () => {
    expect(contrast("#000000", "#ffffff")).toBeCloseTo(21, 5);
    expect(contrast("#777777", "#ffffff")).toBeCloseTo(4.48, 2);
  });
});
