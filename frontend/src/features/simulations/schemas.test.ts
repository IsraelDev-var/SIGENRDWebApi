import { describe, expect, it } from "vitest";

import { formatDistance, formatKw, formatPayback } from "./format";
import { viabilityOf } from "./labels";
import { emptySimulationForm, locationFrom, parseSimulationForm, type SimulationFormValues } from "./schemas";

const valid = (changes: Partial<SimulationFormValues> = {}): SimulationFormValues => ({
  ...emptySimulationForm(),
  monthlyConsumptionKwh: "450",
  installedCapacityKw: "3.5",
  ...changes,
});

describe("formulario del simulador", () => {
  it("con la potencia estimada arma el cuerpo de la API sin equipos", () => {
    const result = parseSimulationForm(valid());
    expect(result.ok && result.body).toMatchObject({
      monthlyConsumptionKwh: 450,
      installedCapacityKw: 3.5,
      equipment: [],
      investmentCost: null,
      flatPricePerKwh: null,
      name: null,
    });
  });

  it("con equipos no manda potencia y distingue paneles de inversores como la API", () => {
    const result = parseSimulationForm(
      valid({
        mode: "equipment",
        installedCapacityKw: "",
        equipment: [
          { kind: "panel", model: "", ratedPowerKw: "0.55", quantity: "8" },
          { kind: "inverter", model: "Growatt 4000", ratedPowerKw: "4", quantity: "1" },
        ],
      }),
    );
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.body.installedCapacityKw).toBeNull();
    expect(result.body.equipment).toEqual([
      { generationSystemType: "Photovoltaic", panelModel: null, inverterModel: null, ratedPowerKw: 0.55, quantity: 8 },
      { generationSystemType: "Photovoltaic", panelModel: null, inverterModel: "Growatt 4000", ratedPowerKw: 4, quantity: 1 },
    ]);
  });

  it("sin paneles no se puede calcular por equipos", () => {
    const result = parseSimulationForm(
      valid({ mode: "equipment", equipment: [{ kind: "inverter", model: "X", ratedPowerKw: "4", quantity: "1" }] }),
    );
    expect(result.ok ? {} : result.errors).toHaveProperty("equipment");
  });

  it("el inversor necesita su modelo", () => {
    const result = parseSimulationForm(
      valid({
        mode: "equipment",
        equipment: [
          { kind: "panel", model: "", ratedPowerKw: "0.5", quantity: "4" },
          { kind: "inverter", model: "", ratedPowerKw: "2", quantity: "1" },
        ],
      }),
    );
    expect(result.ok ? {} : result.errors).toHaveProperty(["equipment.1.model"]);
  });

  it.each([
    ["monthlyConsumptionKwh", "0"],
    ["monthlyConsumptionKwh", "abc"],
    ["installedCapacityKw", "0.05"],
    ["installedCapacityKw", ""],
    ["latitude", "25"],
    ["longitude", "-60"],
    ["investmentCost", "1,000"],
    ["flatPricePerKwh", "0.1"],
  ] as const)("rechaza %s = %j", (field, value) => {
    const result = parseSimulationForm(valid({ [field]: value }));
    expect(result.ok ? {} : result.errors).toHaveProperty(field);
  });

  it("solo acepta una ubicación de la URL dentro del país", () => {
    expect(locationFrom("18.5", "-69.9")).toEqual({ latitude: 18.5, longitude: -69.9 });
    expect(locationFrom("40.7", "-74")).toBeUndefined();
    expect(locationFrom("abc", "-69.9")).toBeUndefined();
    expect(locationFrom(undefined, undefined)).toBeUndefined();
  });
});

describe("resultado", () => {
  it("traduce la clasificación de la API sin calcularla", () => {
    expect(viabilityOf("Profitable").label).toBe("Rentable");
    expect(viabilityOf("NotProfitable").tone).toBe("danger");
    expect(viabilityOf("toString").label).toBe("Sin clasificar");
  });

  it("da formato a retorno, potencia y distancia", () => {
    expect(formatPayback(5.83)).toBe("5.8 años");
    expect(formatPayback(null)).toBe("Sin retorno");
    expect(formatKw(3.5)).toBe("3.5 kWp");
    expect(formatDistance(420.4)).toBe("420 m");
    expect(formatDistance(1530)).toBe("1.5 km");
  });
});
