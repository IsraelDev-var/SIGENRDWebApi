import { describe, expect, it } from "vitest";

import type { NavItem } from "@/config/navigation";

import { findActiveHref } from "./nav-links";

const items: NavItem[] = [
  { href: "/gestion", label: "Panel" },
  { href: "/gestion/solicitudes", label: "Solicitudes" },
  { href: "/gestion/transformadores", label: "Transformadores" },
];

describe("enlace activo del menú", () => {
  it("elige el enlace más específico, no todos los que coinciden por prefijo", () => {
    expect(findActiveHref(items, "/gestion/solicitudes")).toBe("/gestion/solicitudes");
    expect(findActiveHref(items, "/gestion/solicitudes/42")).toBe("/gestion/solicitudes");
  });

  it("marca la página de inicio de la zona en su propia ruta", () => {
    expect(findActiveHref(items, "/gestion")).toBe("/gestion");
  });

  it("no confunde rutas que solo comparten el comienzo del nombre", () => {
    expect(findActiveHref(items, "/gestion-x")).toBeUndefined();
  });

  it("la raíz solo coincide consigo misma", () => {
    expect(findActiveHref([{ href: "/", label: "Mapa" }], "/seguimiento")).toBeUndefined();
    expect(findActiveHref([{ href: "/", label: "Mapa" }], "/")).toBe("/");
  });
});
