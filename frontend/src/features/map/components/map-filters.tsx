"use client";

import { useQuery } from "@tanstack/react-query";

import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { TRANSFORMER_STATUS, TRANSFORMER_STATUSES } from "@/config/status";

import { AVAILABILITY_OPTIONS, useMapState } from "../hooks/use-map-state";
import { fetchZones } from "../lib/map-api";

const selectClass =
  "h-9 w-full rounded-md border border-input bg-card px-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring";

/**
 * Filtros por estado, zona y disponibilidad mínima (RF-03). Se aplican en la API y refrescan el mapa sin recargar
 * la página (CA-04). La distribuidora no se ofrece: en la Fase I solo opera Edesur.
 */
export function MapFilters() {
  const { filters, setFilters, clearFilters, hasFilters } = useMapState();
  const zones = useQuery({ queryKey: ["mapa", "zonas"], queryFn: ({ signal }) => fetchZones(signal), staleTime: 60 * 60 * 1000 });

  return (
    <fieldset className="grid gap-3">
      <legend className="sr-only">Filtros del mapa</legend>
      <div className="grid gap-1.5">
        <Label htmlFor="filtro-estado">Estado</Label>
        <select
          id="filtro-estado"
          className={selectClass}
          value={filters.status ?? ""}
          onChange={(e) => setFilters({ estado: (e.target.value || null) as (typeof TRANSFORMER_STATUSES)[number] | null })}
        >
          <option value="">Todos</option>
          {TRANSFORMER_STATUSES.map((status) => (
            <option key={status} value={status}>
              {TRANSFORMER_STATUS[status].label}
            </option>
          ))}
        </select>
      </div>
      <div className="grid gap-1.5">
        <Label htmlFor="filtro-zona">Zona</Label>
        <select
          id="filtro-zona"
          className={selectClass}
          value={filters.zoneId ?? ""}
          onChange={(e) => setFilters({ zona: e.target.value ? Number(e.target.value) : null })}
          disabled={zones.isPending}
        >
          <option value="">{zones.isError ? "No se pudieron cargar las zonas" : "Todas"}</option>
          {zones.data?.map((zone) => (
            <option key={zone.id} value={zone.id}>
              {zone.zoneName}
              {zone.province ? ` (${zone.province})` : ""}
            </option>
          ))}
        </select>
      </div>
      <div className="grid gap-1.5">
        <Label htmlFor="filtro-disponibilidad">Capacidad libre</Label>
        <select
          id="filtro-disponibilidad"
          className={selectClass}
          value={filters.minAvailability ?? ""}
          onChange={(e) => setFilters({ disp: e.target.value ? Number(e.target.value) : null })}
        >
          <option value="">Cualquiera</option>
          {AVAILABILITY_OPTIONS.map((pct) => (
            <option key={pct} value={pct}>
              {pct} % o más
            </option>
          ))}
        </select>
      </div>
      {hasFilters && (
        <Button type="button" variant="outline" size="sm" onClick={clearFilters} className="justify-self-start">
          Quitar filtros
        </Button>
      )}
    </fieldset>
  );
}
