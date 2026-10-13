import { can, type Action, type Role } from "@/config/permissions";

/** Iconos disponibles para los menús. Son nombres y no componentes para poder pasar los menús a componentes cliente. */
export type NavIcon =
  | "bell"
  | "clipboard-list"
  | "file-text"
  | "gauge"
  | "map"
  | "scroll-text"
  | "search"
  | "sun"
  | "user"
  | "users"
  | "zap";

export type NavItem = {
  href: string;
  label: string;
  icon?: NavIcon;
  /** Acción de la matriz 3.2 que hace falta para ver la opción; sin ella la ve cualquiera de la zona. */
  requires?: Action;
};

/**
 * Menú público.
 */
export const PUBLIC_NAV: NavItem[] = [
  { href: "/", label: "Mapa", icon: "map" },
  { href: "/transformadores", label: "Lista", icon: "clipboard-list" },
  { href: "/seguimiento", label: "Seguimiento", icon: "search" },
  { href: "/requisitos", label: "Requisitos", icon: "file-text" },
];

// Los menús crecen con cada entrega (gestión en F7–F8).
export const PORTAL_NAV: NavItem[] = [
  { href: "/portal", label: "Inicio", icon: "gauge" },
  { href: "/portal/solicitudes", label: "Mis solicitudes", icon: "clipboard-list", requires: "viewOwnRequests" },
  { href: "/portal/simulaciones", label: "Simulaciones", icon: "sun", requires: "manageOwnSimulations" },
  { href: "/portal/perfil", label: "Mi perfil", icon: "user" },
];

export const INTERNAL_NAV: NavItem[] = [{ href: "/gestion", label: "Panel", icon: "gauge" }];

/** Las opciones que el rol puede ver según la matriz de permisos. */
export function navFor(items: NavItem[], role: Role): NavItem[] {
  return items.filter((item) => !item.requires || can(role, item.requires));
}
