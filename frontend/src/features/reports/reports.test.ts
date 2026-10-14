import { describe, expect, it } from "vitest";

import { formatDays, formatMonth, monthlyPoints } from "./format";
import { periodFrom, periodQuery } from "./period";

describe("período de los reportes", () => {
  const today = new Date("2026-10-14T15:00:00Z");

  it("sin fechas son los últimos 12 meses, como la API", () => {
    expect(periodFrom(undefined, undefined, today)).toEqual({ period: { from: "2025-10-14", to: "2026-10-14" }, error: null });
  });

  it("acepta un período válido y descarta fechas mal escritas", () => {
    expect(periodFrom("2026-01-01", "2026-03-31", today).period).toEqual({ from: "2026-01-01", to: "2026-03-31" });
    expect(periodFrom("ayer", "2026-03-31", today).period.from).toBe("2025-03-31");
  });

  it("rechaza fechas al revés y más de 3 años", () => {
    expect(periodFrom("2026-05-01", "2026-04-01", today).error).toMatch(/anterior/);
    expect(periodFrom("2020-01-01", "2026-04-01", today).error).toMatch(/3 años/);
  });

  it("pide a la API hasta el día siguiente para incluir el último", () => {
    expect(periodQuery({ from: "2026-01-01", to: "2026-01-31" }).toString()).toBe("From=2026-01-01&To=2026-02-01");
  });
});

describe("formatos de los reportes", () => {
  it("nombra los meses en español", () => {
    expect(formatMonth("2026-10")).toMatch(/oct.*2026/);
    expect(formatMonth("otra cosa")).toBe("otra cosa");
  });

  it("apila aprobadas, rechazadas y el resto sin contar dos veces", () => {
    expect(monthlyPoints([{ month: "2026-09", total: 10, approved: 6, rejected: 3 }])[0]).toMatchObject({ approved: 6, rejected: 3, others: 1 });
  });

  it("días con un decimal o sin dato", () => {
    expect(formatDays(4.25)).toBe("4.3 días");
    expect(formatDays(null)).toBe("Sin dato");
  });
});
