import "server-only";

import { z } from "zod";

/**
 * Variables solo de servidor. Se validan al usarse (no al importar), para que `next build`
 * de las páginas públicas no las exija.
 */
const serverEnvSchema = z.object({
  /** URL base de la API .NET vista desde el servidor de Next, sin /api/v1. */
  ENERGYMAP_API_URL: z.url(),
  /** Llave pública RSA (PEM, SPKI) con la que se verifica el JWT de la API. Saltos de línea como \n. */
  ENERGYMAP_JWT_PUBLIC_KEY: z.string().includes("BEGIN PUBLIC KEY"),
  ENERGYMAP_JWT_ISSUER: z.string().default("SIGENRD.API"),
  ENERGYMAP_JWT_AUDIENCE: z.string().default("SIGENRD.Client"),
});

export type ServerEnv = z.infer<typeof serverEnvSchema>;

let cached: ServerEnv | undefined;

export function serverEnv(): ServerEnv {
  if (!cached) {
    const parsed = serverEnvSchema.safeParse({
      ...process.env,
      // En una sola línea de variable de entorno el PEM llega con "\n" literales.
      ENERGYMAP_JWT_PUBLIC_KEY: process.env.ENERGYMAP_JWT_PUBLIC_KEY?.replace(/\\n/g, "\n"),
    });
    if (!parsed.success) {
      const missing = parsed.error.issues.map((i) => i.path.join(".")).join(", ");
      throw new Error(`Configuración del servidor incompleta o inválida: ${missing}. Ver frontend/.env.example.`);
    }
    cached = parsed.data;
  }
  return cached;
}
