import "server-only";

import { cache } from "react";

import type { components } from "@/lib/api/schema";
import { ApiError, apiData } from "@/server/api-client";
import type { Session } from "@/server/session";

type Schemas = components["schemas"];
export type Simulation = Schemas["SimulationDto"];
export type SimulationPage = Schemas["SimulationSummaryDtoPagedDto"];
export type SimulationComparison = Schemas["SimulationComparisonDto"];

export const PAGE_SIZE = 20;
export const MAX_COMPARE = 5;

const isId = (value: string) => /^\d{1,10}$/.test(value);

/** Historial propio (RF-26): la API solo devuelve las del usuario. */
export function listMySimulations(session: Session, page: number) {
  return apiData<SimulationPage>(`/Simulations?Page=${page}&PageSize=${PAGE_SIZE}`, { token: session.token });
}

/** null si no existe o es de otra persona: la API responde 404 en los dos casos. */
export const getSimulation = cache(async (session: Session, id: string): Promise<Simulation | null> => {
  if (!isId(id)) return null;
  try {
    return await apiData<Simulation>(`/Simulations/${id}`, { token: session.token });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
});

/** Ids válidos y distintos, de la URL (?ids=1&ids=2). */
export function compareIds(raw: string | string[] | undefined): number[] {
  const values = Array.isArray(raw) ? raw : raw ? [raw] : [];
  return [...new Set(values.filter(isId).map(Number))].slice(0, MAX_COMPARE + 1);
}

export function compareSimulations(session: Session, ids: number[]) {
  const query = new URLSearchParams(ids.map((id) => ["ids", String(id)]));
  return apiData<SimulationComparison>(`/Simulations/compare?${query}`, { token: session.token });
}
