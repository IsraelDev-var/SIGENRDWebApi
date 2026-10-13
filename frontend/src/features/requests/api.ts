import "server-only";

import { cache } from "react";

import type { components } from "@/lib/api/schema";
import { ApiError, apiData } from "@/server/api-client";
import type { Session } from "@/server/session";

type Schemas = components["schemas"];
export type ConnectionRequest = Schemas["ConnectionRequestResponseDto"];
export type RequestPage = Schemas["ConnectionRequestResponseDtoPagedDto"];
export type RequiredDocuments = Schemas["RequiredDocumentsDto"];
export type RequestDocument = Schemas["DocumentDto"];
export type StateHistory = Schemas["StateHistoryDto"];
export type TransformerForRequest = Schemas["TransformerDetailDto"];

export const PAGE_SIZE = 20;

/** Solicitudes del usuario (RN-22: la API solo devuelve las suyas), de la más reciente a la más antigua. */
export function listMyRequests(session: Session, { page, status }: { page: number; status?: string }) {
  const query = new URLSearchParams({ Page: String(page), PageSize: String(PAGE_SIZE) });
  if (status) query.set("Status", status);
  return apiData<RequestPage>(`/ConnectionRequest/search?${query}`, { token: session.token });
}

/** null si no existe o no es del usuario: la API responde 404 en los dos casos (RN-22). */
async function orNull<T>(load: Promise<T>): Promise<T | null> {
  try {
    return await load;
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

export function isRequestId(value: string): boolean {
  return /^\d{1,10}$/.test(value);
}

/** Una sola llamada por petición aunque la usen la página y sus metadatos. */
export const getRequest = cache((session: Session, id: string) =>
  isRequestId(id) ? orNull(apiData<ConnectionRequest>(`/ConnectionRequest/${id}`, { token: session.token })) : Promise.resolve(null),
);

/** Lo que se muestra en el expediente, en paralelo. */
export async function getRequestFile(session: Session, id: number) {
  const options = { token: session.token };
  const [required, documents, history] = await Promise.all([
    apiData<RequiredDocuments>(`/ConnectionRequest/${id}/required-documents`, options),
    apiData<RequestDocument[]>(`/ConnectionRequest/${id}/documents`, options),
    apiData<StateHistory[]>(`/ConnectionRequest/${id}/history`, options),
  ]);
  return { required, documents, history };
}

export function getTransformer(id: string) {
  return isRequestId(id) ? orNull(apiData<TransformerForRequest>(`/Transformers/${id}`)) : Promise.resolve(null);
}
