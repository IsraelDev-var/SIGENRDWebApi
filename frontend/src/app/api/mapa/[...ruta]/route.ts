import { NextResponse, type NextRequest } from "next/server";

import { serverEnv } from "@/server/env";

/**
 * Proxy de solo lectura hacia los endpoints públicos del mapa de la API. El navegador llama a /api/mapa/… del propio
 * sitio: la URL de la API se configura al ejecutar (no al compilar) y no hace falta CORS. Solo deja pasar esta lista.
 */
const ALLOWED: RegExp[] = [
  /^Transformers$/, // GET área visible (RF-01)
  /^Transformers\/clusters$/, // grupos por zoom (B-2)
  /^Transformers\/search$/, // por código o coordenadas (RF-04)
  /^Transformers\/\d{1,10}$/, // detalle (RF-05)
  /^ServiceZones$/, // catálogos (B-1)
  /^Distributors$/,
];

const MAX_QUERY_LENGTH = 1024;

export async function GET(request: NextRequest, { params }: RouteContext<"/api/mapa/[...ruta]">) {
  const { ruta } = await params;
  const path = ruta.join("/");
  const query = request.nextUrl.search;

  if (!ALLOWED.some((pattern) => pattern.test(path)) || query.length > MAX_QUERY_LENGTH) {
    return NextResponse.json({ succeeded: false, message: "Ruta no disponible." }, { status: 404 });
  }

  let response: Response;
  try {
    response = await fetch(`${serverEnv().ENERGYMAP_API_URL.replace(/\/$/, "")}/api/v1/${path}${query}`, {
      headers: { Accept: "application/json" },
      cache: "no-store",
      signal: AbortSignal.timeout(10_000),
    });
  } catch {
    return NextResponse.json({ succeeded: false, message: "No pudimos cargar el mapa. Revisa tu conexión e inténtalo de nuevo." }, { status: 503 });
  }

  return new NextResponse(response.body, {
    status: response.status,
    headers: {
      "Content-Type": response.headers.get("Content-Type") ?? "application/json",
      // Los datos del mapa son públicos e iguales para todos; la API los cachea 30 s (RNF-01).
      "Cache-Control": response.ok ? (response.headers.get("Cache-Control") ?? "public, max-age=30") : "no-store",
    },
  });
}
