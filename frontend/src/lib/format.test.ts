import { describe, expect, it } from "vitest";

import { daysBetween, formatDaysAgo, formatKva, formatMoney, formatPercent } from "@/lib/format";

describe("formatos es-DO", () => {
  it("kVA con punto decimal y coma de miles", () => {
    expect(formatKva(1250.55)).toBe("1,250.6 kVA");
    expect(formatKva(75)).toBe("75 kVA");
  });

  it("porcentaje con un decimal, también negativo", () => {
    expect(formatPercent(52.345)).toBe("52.3 %");
    expect(formatPercent(-4)).toBe("-4.0 %");
  });

  it("pesos dominicanos sin centavos", () => {
    expect(formatMoney(60000)).toBe("RD$60,000");
  });
});

describe("antigüedad del dato", () => {
  const now = new Date("2026-10-12T15:00:00Z");

  it("cuenta días completos", () => {
    expect(daysBetween("2026-10-12T08:00:00Z", now)).toBe(0);
    expect(daysBetween("2026-09-30T15:00:00Z", now)).toBe(12);
  });

  it("no da días negativos con una fecha futura", () => {
    expect(daysBetween("2026-10-20T00:00:00Z", now)).toBe(0);
  });

  it("lo dice en palabras", () => {
    expect(formatDaysAgo("2026-10-12T08:00:00Z", now)).toBe("hoy");
    expect(formatDaysAgo("2026-10-11T08:00:00Z", now)).toBe("ayer");
    expect(formatDaysAgo("2026-09-30T15:00:00Z", now)).toBe("hace 12 días");
  });
});
