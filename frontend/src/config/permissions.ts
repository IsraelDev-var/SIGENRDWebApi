/**
 * Matriz de permisos 3.2 del documento EnergyMap v1.2, en un solo lugar (RF-38, RF-39).
 * La interfaz la usa para mostrar u ocultar acciones y proteger rutas; la API decide siempre.
 */

export const ROLES = ["Customer", "InstallerCompany", "Engineer", "Distributor", "Admin"] as const;
export type Role = (typeof ROLES)[number];

export const ROLE_LABEL: Record<Role, string> = {
  Customer: "Cliente",
  InstallerCompany: "Instaladora",
  Engineer: "Ingeniero",
  Distributor: "Distribuidora",
  Admin: "Administrador",
};

export function isRole(value: unknown): value is Role {
  return typeof value === "string" && (ROLES as readonly string[]).includes(value);
}

/** Acciones de la matriz 3.2, en el mismo orden que el documento. */
export type Action =
  | "viewMap"
  | "manageOwnSimulations"
  | "createRequest"
  | "uploadOwnDocuments"
  | "viewOwnRequests"
  | "viewOthersRequests"
  | "reviewRequests"
  | "decideRequests"
  | "validateInstallers"
  | "manageCatalogs"
  | "viewReports"
  | "manageInternalUsers"
  | "viewAudit"
  | "modifyApprovedRequest";

const APPLICANTS: Role[] = ["Customer", "InstallerCompany"];
const STAFF: Role[] = ["Engineer", "Distributor", "Admin"];
const DECIDERS: Role[] = ["Distributor", "Admin"];

/** Roles autenticados que pueden hacer cada acción. "public" = también el visitante anónimo. */
export const PERMISSIONS: Record<Action, readonly Role[] | "public"> = {
  viewMap: "public",
  manageOwnSimulations: APPLICANTS,
  // La instaladora además debe estar validada (RN-05): es una condición de datos, no de rol.
  createRequest: APPLICANTS,
  uploadOwnDocuments: APPLICANTS,
  viewOwnRequests: APPLICANTS,
  viewOthersRequests: STAFF,
  reviewRequests: STAFF,
  decideRequests: DECIDERS,
  validateInstallers: DECIDERS,
  manageCatalogs: DECIDERS,
  viewReports: DECIDERS,
  manageInternalUsers: ["Admin"],
  viewAudit: ["Admin"],
  // RN-07: nadie modifica ni borra una solicitud aprobada.
  modifyApprovedRequest: [],
};

/** `role` null = visitante anónimo. */
export function can(role: Role | null, action: Action): boolean {
  const allowed = PERMISSIONS[action];
  if (allowed === "public") return true;
  return role !== null && allowed.includes(role);
}

/** Zonas privadas de la app y quién entra en cada una. */
export const ZONES = {
  portal: { path: "/portal", roles: APPLICANTS },
  gestion: { path: "/gestion", roles: STAFF },
} as const satisfies Record<string, { path: string; roles: readonly Role[] }>;

export type Zone = keyof typeof ZONES;

export function zoneOf(pathname: string): Zone | null {
  for (const [zone, { path }] of Object.entries(ZONES) as [Zone, (typeof ZONES)[Zone]][]) {
    if (pathname === path || pathname.startsWith(`${path}/`)) return zone;
  }
  return null;
}

export function canEnterZone(role: Role, zone: Zone): boolean {
  return (ZONES[zone].roles as readonly Role[]).includes(role);
}

/** Página de inicio de cada rol después de iniciar sesión. */
export function homeFor(role: Role): string {
  return canEnterZone(role, "portal") ? ZONES.portal.path : ZONES.gestion.path;
}
