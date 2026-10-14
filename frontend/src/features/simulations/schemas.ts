import { z } from "zod";

import type { components } from "@/lib/api/schema";

/**
 * Formulario del simulador: repite las reglas de CreateSimulationValidator para avisar antes de enviar. El cálculo
 * lo hace siempre la API (fórmula versionada); aquí no se estima ningún resultado.
 */

export type SimulationBody = components["schemas"]["CreateSimulationCommand"];

export type EquipmentValues = {
  /** Panel: suma a la potencia instalada. Inversor: solo se compara con los paneles (RF-24). */
  kind: "panel" | "inverter";
  model: string;
  ratedPowerKw: string;
  quantity: string;
};

export type SimulationFormValues = {
  name: string;
  monthlyConsumptionKwh: string;
  latitude: string;
  longitude: string;
  /** Potencia estimada o detalle de los equipos. */
  mode: "capacity" | "equipment";
  installedCapacityKw: string;
  equipment: EquipmentValues[];
  investmentCost: string;
  flatPricePerKwh: string;
};

export const MAX_EQUIPMENT = 20;
export const EMPTY_PANEL: EquipmentValues = { kind: "panel", model: "", ratedPowerKw: "", quantity: "1" };

/** Centro del Distrito Nacional: punto de partida si no llega una ubicación. */
const DEFAULT_LOCATION = { latitude: 18.4861, longitude: -69.9312 };

export function emptySimulationForm(location: { latitude: number; longitude: number } = DEFAULT_LOCATION): SimulationFormValues {
  return {
    name: "",
    monthlyConsumptionKwh: "",
    latitude: location.latitude.toFixed(6),
    longitude: location.longitude.toFixed(6),
    mode: "capacity",
    installedCapacityKw: "",
    equipment: [{ ...EMPTY_PANEL }],
    investmentCost: "",
    flatPricePerKwh: "",
  };
}

/** Ubicación de la URL (?lat=&lng=) solo si es un número dentro de República Dominicana. */
export function locationFrom(lat: string | undefined, lng: string | undefined) {
  const latitude = Number(lat);
  const longitude = Number(lng);
  if (!lat || !lng || !Number.isFinite(latitude) || !Number.isFinite(longitude)) return undefined;
  return inDominicanRepublic(latitude, longitude) ? { latitude, longitude } : undefined;
}

function inDominicanRepublic(latitude: number, longitude: number) {
  return latitude >= 17.3 && latitude <= 20.1 && longitude >= -72.1 && longitude <= -68.2;
}

const number = (message: string) =>
  z
    .string()
    .trim()
    .min(1, message)
    .regex(/^\d+(\.\d+)?$/, message)
    .transform(Number);

const optionalNumber = (message: string) =>
  z
    .string()
    .trim()
    .regex(/^(\d+(\.\d+)?)?$/, message)
    .transform((v) => (v === "" ? null : Number(v)));

const OUTSIDE = "La ubicación debe estar en República Dominicana.";

const baseSchema = z.object({
  name: z
    .string()
    .trim()
    .max(100, "Máximo 100 caracteres.")
    .transform((v) => (v === "" ? null : v)),
  monthlyConsumptionKwh: number("Escribe tu consumo mensual en kWh, por ejemplo 450.").pipe(
    z.number().min(1, "El consumo debe estar entre 1 y 1,000,000 kWh.").max(1_000_000, "El consumo debe estar entre 1 y 1,000,000 kWh."),
  ),
  latitude: z
    .string()
    .trim()
    .regex(/^-?\d+(\.\d+)?$/, "Escribe la latitud, por ejemplo 18.4861.")
    .transform(Number)
    .pipe(z.number().min(17.3, OUTSIDE).max(20.1, OUTSIDE)),
  longitude: z
    .string()
    .trim()
    .regex(/^-?\d+(\.\d+)?$/, "Escribe la longitud, por ejemplo -69.9312.")
    .transform(Number)
    .pipe(z.number().min(-72.1, OUTSIDE).max(-68.2, OUTSIDE)),
  investmentCost: optionalNumber("Escribe el costo en pesos, sin comas.").pipe(
    z.number().gt(0, "El costo debe ser mayor que cero.").max(1_000_000_000, "El costo es demasiado alto.").nullable(),
  ),
  flatPricePerKwh: optionalNumber("Escribe el precio con punto decimal, por ejemplo 12.5.").pipe(
    z.number().min(0.5, "Entre RD$0.50 y RD$100.").max(100, "Entre RD$0.50 y RD$100.").nullable(),
  ),
});

const equipmentSchema = z.object({
  kind: z.enum(["panel", "inverter"]),
  model: z.string().trim().max(150, "Máximo 150 caracteres."),
  ratedPowerKw: number("Escribe la potencia en kW.").pipe(z.number().min(0.01, "Entre 0.01 y 1,000 kW.").max(1000, "Entre 0.01 y 1,000 kW.")),
  quantity: z
    .string()
    .trim()
    .regex(/^\d+$/, "Escribe una cantidad entera.")
    .transform(Number)
    .pipe(z.number().min(1, "Entre 1 y 10,000.").max(10_000, "Entre 1 y 10,000.")),
});

export type FieldErrors = Record<string, string[]>;
export type ParsedSimulation = { ok: true; body: SimulationBody } | { ok: false; errors: FieldErrors };

export function parseSimulationForm(values: SimulationFormValues): ParsedSimulation {
  const errors: FieldErrors = {};
  const add = (key: string, message: string) => (errors[key] ??= [message]);

  const base = baseSchema.safeParse(values);
  if (!base.success) for (const issue of base.error.issues) add(issue.path.join("."), issue.message);

  let installedCapacityKw: number | null = null;
  const equipment: SimulationBody["equipment"] = [];

  if (values.mode === "capacity") {
    const capacity = number("Escribe la potencia estimada en kWp, por ejemplo 3.5.")
      .pipe(z.number().min(0.1, "Entre 0.1 y 1,000 kWp.").max(1000, "Entre 0.1 y 1,000 kWp."))
      .safeParse(values.installedCapacityKw);
    if (capacity.success) installedCapacityKw = capacity.data;
    else add("installedCapacityKw", capacity.error.issues[0]!.message);
  } else {
    if (values.equipment.length > MAX_EQUIPMENT) add("equipment", `Se admiten hasta ${MAX_EQUIPMENT} equipos.`);
    if (!values.equipment.some((e) => e.kind === "panel")) add("equipment", "Registra al menos una fila de paneles.");
    values.equipment.forEach((row, i) => {
      const parsed = equipmentSchema.safeParse(row);
      if (!parsed.success) {
        for (const issue of parsed.error.issues) add(`equipment.${i}.${issue.path.join(".")}`, issue.message);
        return;
      }
      // La API distingue el inversor porque solo trae modelo de inversor.
      if (parsed.data.kind === "inverter" && !parsed.data.model) {
        add(`equipment.${i}.model`, "Escribe el modelo del inversor.");
        return;
      }
      equipment.push({
        generationSystemType: "Photovoltaic",
        panelModel: parsed.data.kind === "panel" ? parsed.data.model || null : null,
        inverterModel: parsed.data.kind === "inverter" ? parsed.data.model : null,
        ratedPowerKw: parsed.data.ratedPowerKw,
        quantity: parsed.data.quantity,
      });
    });
  }

  if (Object.keys(errors).length > 0 || !base.success) return { ok: false, errors };
  return { ok: true, body: { ...base.data, installedCapacityKw, equipment } };
}
