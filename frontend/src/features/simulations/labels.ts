import type { Tone } from "@/config/status";

/** Clasificación de la simulación (RF-21). Los umbrales los decide la API; aquí solo se traduce el valor. */
export const VIABILITY: Record<string, { label: string; tone: Tone; meaning: string }> = {
  Profitable: { label: "Rentable", tone: "success", meaning: "La inversión se recupera en poco tiempo con el ahorro estimado." },
  ConditionallyProfitable: {
    label: "Condicionalmente rentable",
    tone: "warning",
    meaning: "Se recupera, pero en un plazo largo. Revisa la cotización o la potencia.",
  },
  NotProfitable: { label: "No rentable", tone: "danger", meaning: "Con estos datos el ahorro no alcanza para recuperar la inversión." },
};

export function viabilityOf(value: string) {
  return Object.hasOwn(VIABILITY, value) ? VIABILITY[value]! : { label: "Sin clasificar", tone: "neutral" as Tone, meaning: "" };
}
