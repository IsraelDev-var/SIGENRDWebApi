import "server-only";

import type { components } from "@/lib/api/schema";
import { ApiError, apiData } from "@/server/api-client";
import type { Session } from "@/server/session";

type Schemas = components["schemas"];
export type RequestPage = Schemas["ConnectionRequestResponseDtoPagedDto"];
export type TransformerPage = Schemas["TransformerDetailDtoPagedDto"];
export type Transformer = Schemas["TransformerDetailDto"];
export type InstallerCompany = Schemas["InstallerCompanyDto"];
export type ServiceZone = Schemas["ServiceZoneDto"];
export type Distributor = Schemas["DistributorDto"];

export const PAGE_SIZE = 25;

/** Estados que trabaja el personal: el borrador no le llega (D-4). */
export const QUEUE_STATUSES = ["Received", "UnderReview", "Observed", "InCorrection", "Approved", "Rejected", "Cancelled"] as const;
/** Lo que espera una acción del personal. */
export const PENDING_STATUSES = ["Received", "UnderReview"] as const;

export type RequestFilters = { status?: string; type?: string; text?: string; from?: string; to?: string; page: number };

/** Bandeja de revisión (RF-12, RF-14): la API ya limita a lo que el rol puede ver y excluye borradores. */
export function listRequests(session: Session, filters: RequestFilters) {
  const query = new URLSearchParams({ Page: String(filters.page), PageSize: String(PAGE_SIZE) });
  if (filters.status) query.set("Status", filters.status);
  if (filters.type) query.set("RequestType", filters.type);
  if (filters.text) query.set("Text", filters.text);
  if (filters.from) query.set("From", filters.from);
  if (filters.to) query.set("To", `${filters.to}T23:59:59`);
  return apiData<RequestPage>(`/ConnectionRequest/search?${query}`, { token: session.token });
}

/** Cuántas solicitudes hay en cada estado de la cola, para el panel. Una consulta mínima por estado. */
export async function queueCounts(session: Session, statuses: readonly string[]) {
  const counts = await Promise.all(
    statuses.map(async (status) => {
      const page = await apiData<RequestPage>(`/ConnectionRequest/search?Status=${status}&PageSize=1`, { token: session.token });
      return [status, page.totalCount] as const;
    }),
  );
  return Object.fromEntries(counts) as Record<string, number>;
}

export type TransformerFilters = { code?: string; zone?: string; status?: string; sort?: string; page: number };

/** Lista de la red (B-3). */
export function listTransformers(session: Session, filters: TransformerFilters) {
  const query = new URLSearchParams({ Page: String(filters.page), PageSize: String(PAGE_SIZE) });
  if (filters.code) query.set("Code", filters.code);
  if (filters.zone) query.set("ServiceZoneId", filters.zone);
  if (filters.status) query.set("Status", filters.status);
  if (filters.sort) query.set("Sort", filters.sort);
  return apiData<TransformerPage>(`/Transformers/manage?${query}`, { token: session.token });
}

export async function getTransformerById(id: string): Promise<Transformer | null> {
  if (!/^\d{1,10}$/.test(id)) return null;
  try {
    return await apiData<Transformer>(`/Transformers/${id}`);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

export function listZones() {
  return apiData<ServiceZone[]>("/ServiceZones");
}

export function listDistributors() {
  return apiData<Distributor[]>("/Distributors");
}

export function listInstallers(session: Session, status?: string) {
  return apiData<InstallerCompany[]>(`/InstallerCompanies${status ? `?Status=${status}` : ""}`, { token: session.token });
}
