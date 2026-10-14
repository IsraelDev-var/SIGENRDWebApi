import "server-only";

import type { components } from "@/lib/api/schema";
import { apiData } from "@/server/api-client";
import type { Session } from "@/server/session";

type Schemas = components["schemas"];
export type InternalUser = Schemas["InternalUserDto"];
export type UserPage = Schemas["InternalUserDtoPagedDto"];
export type AuditPage = Schemas["AuditLogDtoPagedDto"];

export const INTERNAL_ROLES = ["Engineer", "Distributor", "Admin"] as const;
export type InternalRole = (typeof INTERNAL_ROLES)[number];
export const PAGE_SIZE = 25;

/** Usuarios internos (B-5): solo el administrador; la API lo vuelve a comprobar. */
export function listUsers(session: Session, filters: { text?: string; role?: string; page: number }) {
  const query = new URLSearchParams({ page: String(filters.page), pageSize: String(PAGE_SIZE) });
  if (filters.text) query.set("text", filters.text);
  if (filters.role) query.set("role", filters.role);
  return apiData<UserPage>(`/Users?${query}`, { token: session.token });
}

export type AuditFilters = { entity?: string; entityId?: string; action?: string; userId?: string; from?: string; to?: string; page: number };

/** Registro de auditoría (RN-20, RF-40), del evento más reciente al más antiguo. */
export function searchAudit(session: Session, filters: AuditFilters) {
  const query = new URLSearchParams({ Page: String(filters.page), PageSize: String(PAGE_SIZE) });
  if (filters.entity) query.set("EntityName", filters.entity);
  if (filters.entityId) query.set("EntityId", filters.entityId);
  if (filters.action) query.set("Action", filters.action);
  if (filters.userId) query.set("UserId", filters.userId);
  if (filters.from) query.set("From", filters.from);
  if (filters.to) query.set("To", `${filters.to}T23:59:59`);
  return apiData<AuditPage>(`/AuditLogs?${query}`, { token: session.token });
}
