import type { NavItem } from "@/config/navigation";

// Menús de ejemplo para el catálogo. Los reales se derivan de los permisos de cada rol en F2.
export const SAMPLE_PORTAL_NAV: NavItem[] = [
  { href: "/catalogo/portal", label: "Inicio", icon: "gauge" },
  { href: "/catalogo/portal/solicitudes", label: "Mis solicitudes", icon: "file-text" },
  { href: "/catalogo/portal/simulaciones", label: "Simulaciones", icon: "sun" },
  { href: "/catalogo/portal/notificaciones", label: "Notificaciones", icon: "bell" },
];

export const SAMPLE_INTERNAL_NAV: NavItem[] = [
  { href: "/catalogo/gestion", label: "Panel", icon: "gauge" },
  { href: "/catalogo/gestion/solicitudes", label: "Solicitudes", icon: "clipboard-list" },
  { href: "/catalogo/gestion/transformadores", label: "Transformadores", icon: "zap" },
  { href: "/catalogo/gestion/zonas", label: "Zonas críticas", icon: "map" },
  { href: "/catalogo/gestion/usuarios", label: "Usuarios", icon: "users" },
  { href: "/catalogo/gestion/auditoria", label: "Auditoría", icon: "scroll-text" },
];

export const CATALOG_PAGES: NavItem[] = [
  { href: "/catalogo", label: "Componentes" },
  { href: "/catalogo/publica", label: "Zona pública" },
  { href: "/catalogo/portal", label: "Portal" },
  { href: "/catalogo/gestion", label: "Gestión" },
];
