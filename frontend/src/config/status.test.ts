import { describe, expect, it } from "vitest";

import { asRequestStatus, asTransformerStatus, REQUEST_STATUS, TRANSFORMER_STATUS } from "@/config/status";

describe("estados del transformador", () => {
  it("cubre los cinco estados de RN-02 con las etiquetas del documento", () => {
    expect(Object.fromEntries(Object.entries(TRANSFORMER_STATUS).map(([k, v]) => [k, v.label]))).toEqual({
      Available: "Disponible",
      Conditioned: "Condicionada",
      Critical: "Crítica",
      Saturated: "Saturada",
      Maintenance: "Mantenimiento",
    });
  });

  it("distingue Crítica de Saturada por icono y color, no solo por texto", () => {
    expect(TRANSFORMER_STATUS.Critical.icon).not.toBe(TRANSFORMER_STATUS.Saturated.icon);
    expect(TRANSFORMER_STATUS.Critical.fillClass).not.toBe(TRANSFORMER_STATUS.Saturated.fillClass);
  });

  it("usa un icono distinto para cada estado", () => {
    const icons = Object.values(TRANSFORMER_STATUS).map((s) => s.icon);
    expect(new Set(icons).size).toBe(icons.length);
  });

  it("acepta solo valores conocidos de la API", () => {
    expect(asTransformerStatus("Critical")).toBe("Critical");
    expect(asTransformerStatus("critical")).toBeNull();
    expect(asTransformerStatus("toString")).toBeNull();
    expect(asTransformerStatus(undefined)).toBeNull();
  });
});

describe("estados de solicitud", () => {
  it("cubre los siete estados de RN-12 y el borrador de D-4", () => {
    expect(Object.values(REQUEST_STATUS).map((s) => s.label)).toEqual([
      "Borrador",
      "Recibida",
      "En revisión",
      "Observaciones",
      "En corrección",
      "Aprobada",
      "Rechazada",
      "Cancelada",
    ]);
  });

  it("acepta solo valores conocidos de la API", () => {
    expect(asRequestStatus("UnderReview")).toBe("UnderReview");
    expect(asRequestStatus("Draft")).toBe("Draft");
    expect(asRequestStatus("draft")).toBeNull();
    expect(asRequestStatus("toString")).toBeNull();
  });
});
