import "server-only";

import type { components } from "@/lib/api/schema";
import { ApiError, apiData } from "@/server/api-client";
import type { Session } from "@/server/session";

export type CustomerProfile = components["schemas"]["CustomerProfileDto"];
export type InstallerCompany = components["schemas"]["InstallerCompanyDto"];

export type ApplicantProfile =
  | { kind: "customer"; profile: CustomerProfile | null }
  | { kind: "installer"; company: InstallerCompany | null };

/**
 * Perfil del solicitante. Hace falta para crear solicitudes; el registro solo crea la cuenta,
 * así que tras el primer ingreso puede no existir (la API responde 404).
 */
export async function getApplicantProfile(session: Session): Promise<ApplicantProfile> {
  const isInstaller = session.role === "InstallerCompany";
  try {
    if (isInstaller) {
      return { kind: "installer", company: await apiData<InstallerCompany>("/InstallerCompanies/me", { token: session.token }) };
    }
    return { kind: "customer", profile: await apiData<CustomerProfile>("/Customers/me", { token: session.token }) };
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return isInstaller ? { kind: "installer", company: null } : { kind: "customer", profile: null };
    }
    throw error;
  }
}

/** RN-05: una instaladora solo crea solicitudes cuando la distribuidora la valida. */
export function isInstallerValidated(company: InstallerCompany | null): boolean {
  return company?.validationStatus === "Validated";
}
