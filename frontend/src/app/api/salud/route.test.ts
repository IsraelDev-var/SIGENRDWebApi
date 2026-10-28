import { generateKeyPairSync } from "node:crypto";

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// En las pruebas no hay límite entre servidor y cliente que proteger.
vi.mock("server-only", () => ({}));

const publicKeyPem = generateKeyPairSync("rsa", { modulusLength: 2048 })
  .publicKey.export({ type: "spki", format: "pem" })
  .toString();

// serverEnv() guarda la configuración la primera vez: cada prueba importa la ruta de nuevo.
async function getHealth() {
  vi.resetModules();
  const { GET } = await import("./route");
  return GET();
}

describe("GET /api/salud", () => {
  beforeEach(() => {
    vi.stubEnv("ENERGYMAP_API_URL", "");
    vi.stubEnv("ENERGYMAP_JWT_PUBLIC_KEY", "");
  });
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("responde Healthy con la configuración completa, sin guardarse en caché", async () => {
    vi.stubEnv("ENERGYMAP_API_URL", "http://api.interna:8080");
    vi.stubEnv("ENERGYMAP_JWT_PUBLIC_KEY", publicKeyPem.replace(/\n/g, "\\n"));
    const response = await getHealth();
    expect(response.status).toBe(200);
    expect(await response.json()).toEqual({ status: "Healthy" });
    expect(response.headers.get("Cache-Control")).toBe("no-store");
  });

  it("responde 503 sin la configuración del servidor y no dice cuál falta", async () => {
    const response = await getHealth();
    expect(response.status).toBe(503);
    expect(await response.json()).toEqual({ status: "Unhealthy" });
  });
});
