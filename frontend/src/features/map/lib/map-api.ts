import type { components } from "@/lib/api/schema";

import type { Bounds } from "./viewport";

/** Llamadas del navegador al proxy del mapa (/api/mapa/…, ver app/api/mapa). */

export type TransformerDetail = components["schemas"]["TransformerDetailDto"];
export type MapPointsResult = components["schemas"]["TransformerMapResultDto"];
export type ClustersResult = components["schemas"]["TransformerClusterResultDto"];
export type MapPoint = components["schemas"]["TransformerMapPointDto"];
export type ServiceZone = components["schemas"]["ServiceZoneDto"];

export type MapFilters = {
  status: string | null;
  zoneId: number | null;
  minAvailability: number | null;
};

export class MapApiError extends Error {}

async function get<T>(path: string, params: Record<string, string | number | null | undefined>, signal?: AbortSignal): Promise<T> {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== null && value !== undefined && value !== "") query.set(key, String(value));
  }
  const suffix = query.size > 0 ? `?${query}` : "";

  let response: Response;
  try {
    response = await fetch(`/api/mapa/${path}${suffix}`, { signal });
  } catch (error) {
    if (signal?.aborted) throw error;
    throw new MapApiError("No pudimos cargar los transformadores. Revisa tu conexión.");
  }

  const json = (await response.json().catch(() => null)) as { data?: T; message?: string } | null;
  if (!response.ok || !json) {
    throw new MapApiError(response.status === 404 ? "No encontramos ese transformador." : (json?.message ?? "No pudimos cargar los datos del mapa."));
  }
  return json.data as T;
}

const filterParams = (f: MapFilters) => ({ status: f.status, serviceZoneId: f.zoneId, minAvailabilityPct: f.minAvailability });

export function fetchClusters(bounds: Bounds, zoom: number, filters: MapFilters, signal?: AbortSignal) {
  return get<ClustersResult>("Transformers/clusters", { ...bounds, zoom, ...filterParams(filters) }, signal);
}

export function fetchPoints(bounds: Bounds, filters: MapFilters, signal?: AbortSignal) {
  return get<MapPointsResult>("Transformers", { ...bounds, ...filterParams(filters) }, signal);
}

export function fetchTransformer(id: number, signal?: AbortSignal) {
  return get<TransformerDetail>(`Transformers/${id}`, {}, signal);
}

export function searchByCode(code: string, signal?: AbortSignal) {
  return get<MapPoint[]>("Transformers/search", { code, limit: 10 }, signal);
}

export function searchNear(latitude: number, longitude: number, signal?: AbortSignal) {
  return get<MapPoint[]>("Transformers/search", { latitude, longitude, radiusMeters: 2000, limit: 10 }, signal);
}

export function fetchZones(signal?: AbortSignal) {
  return get<ServiceZone[]>("ServiceZones", {}, signal);
}
