import { describe, expect, it } from "vitest";

import { contentSecurityPolicy, newNonce } from "./security";

describe("Content Security Policy", () => {
  const policy = contentSecurityPolicy("abc123", { dev: false, https: true });
  const directive = (name: string) => policy.split("; ").find((d) => d.startsWith(`${name} `)) ?? "";

  it("los scripts solo con el nonce: sin unsafe-inline ni unsafe-eval en producción", () => {
    expect(directive("script-src")).toBe("script-src 'self' 'nonce-abc123' 'strict-dynamic'");
  });

  it("no se puede incrustar, ni cargar objetos, ni cambiar la base", () => {
    expect(policy).toContain("frame-ancestors 'none'");
    expect(policy).toContain("object-src 'none'");
    expect(policy).toContain("base-uri 'self'");
    expect(policy).toContain("form-action 'self'");
  });

  it("deja al mapa pedir sus teselas y crear su worker", () => {
    expect(directive("connect-src")).toContain("https://*.basemaps.cartocdn.com");
    expect(directive("worker-src")).toBe("worker-src 'self' blob:");
  });

  it("permite el origen de los estilos del mapa configurados (proveedor de producción, D-2)", () => {
    const withMapTiler = contentSecurityPolicy("n", {
      dev: false,
      https: true,
      mapStyles: ["https://api.maptiler.com/maps/streets-v2/style.json?key=abc", "https://api.maptiler.com/maps/dark/style.json"],
    });
    const connect = withMapTiler.split("; ").find((d) => d.startsWith("connect-src ")) ?? "";
    expect(connect).toBe("connect-src 'self' https://basemaps.cartocdn.com https://*.basemaps.cartocdn.com https://api.maptiler.com");
    expect(withMapTiler).toContain("img-src 'self' data: blob: https://basemaps.cartocdn.com https://*.basemaps.cartocdn.com https://api.maptiler.com");
  });

  it("en desarrollo permite eval y el websocket de recarga; por HTTP no fuerza HTTPS", () => {
    const dev = contentSecurityPolicy("n", { dev: true, https: false });
    expect(dev).toContain("'unsafe-eval'");
    expect(dev).toContain("ws:");
    expect(dev).not.toContain("upgrade-insecure-requests");
    expect(policy).toContain("upgrade-insecure-requests");
  });

  it("cada nonce es distinto y tiene 128 bits", () => {
    const a = newNonce();
    expect(a).not.toBe(newNonce());
    expect(atob(a)).toHaveLength(16);
  });
});
