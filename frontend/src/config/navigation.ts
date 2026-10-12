/** Iconos disponibles para los menús. Son nombres y no componentes para poder pasar los menús a componentes cliente. */
export type NavIcon =
  | "bell"
  | "clipboard-list"
  | "file-text"
  | "gauge"
  | "map"
  | "scroll-text"
  | "sun"
  | "users"
  | "zap";

export type NavItem = {
  href: string;
  label: string;
  icon?: NavIcon;
};

/**
 * Menú público. Solo enlaza a páginas que existen: Seguimiento y Requisitos se añaden en F4 e Ingresar en F2.
 * Los menús del portal y de gestión dependen del rol y se derivan de los permisos en F2.
 */
export const PUBLIC_NAV: NavItem[] = [{ href: "/", label: "Mapa", icon: "map" }];
