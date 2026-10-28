import { serverEnv } from "@/server/env";

/**
 * Salud del servidor de Next para el health check de App Service y la prueba de humo del CD (F9-01): responde y tiene
 * su configuración completa. No llama a la API, que tiene su propio /health. El motivo no se detalla: es una ruta pública.
 */
export function GET() {
  const headers = { "Cache-Control": "no-store" };
  try {
    serverEnv();
    return Response.json({ status: "Healthy" }, { headers });
  } catch {
    return Response.json({ status: "Unhealthy" }, { status: 503, headers });
  }
}
