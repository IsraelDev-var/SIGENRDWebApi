"use client";

import { parseAsInteger, parseAsStringLiteral, useQueryStates } from "nuqs";

import { TRANSFORMER_STATUSES } from "@/config/status";

import type { MapFilters } from "../lib/map-api";

/** Umbrales del filtro de disponibilidad: los de RN-02 (Condicionada desde 30 %, Disponible desde 70 %). */
export const AVAILABILITY_OPTIONS = [30, 70] as const;

/**
 * Filtros y transformador elegido en la URL (?estado=Critical&zona=3&disp=30&t=42): se pueden compartir,
 * sobreviven a recargar la página y funcionan con el botón Atrás.
 */
export function useMapState() {
  const [state, setState] = useQueryStates(
    {
      estado: parseAsStringLiteral(TRANSFORMER_STATUSES),
      zona: parseAsInteger,
      disp: parseAsInteger,
      t: parseAsInteger,
    },
    { history: "replace" },
  );

  const filters: MapFilters = {
    status: state.estado,
    zoneId: state.zona,
    minAvailability: state.disp != null && (AVAILABILITY_OPTIONS as readonly number[]).includes(state.disp) ? state.disp : null,
  };

  return {
    filters,
    selectedId: state.t,
    select: (id: number | null) => setState({ t: id }, { history: "push" }),
    setFilters: (next: Partial<{ estado: (typeof TRANSFORMER_STATUSES)[number] | null; zona: number | null; disp: number | null }>) =>
      setState(next),
    clearFilters: () => setState({ estado: null, zona: null, disp: null }),
    hasFilters: state.estado != null || state.zona != null || state.disp != null,
  };
}
