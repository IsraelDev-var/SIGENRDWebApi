import "server-only";

import { ApiError, apiResponse } from "@/server/api-client";
import { getSession } from "@/server/session";

const PASSTHROUGH = ["content-type", "content-length", "content-disposition"];

/**
 * Reenvía al navegador un archivo de la API con el token de la sesión. Los errores salen como texto corto: el
 * enlace se abre fuera de la página y no hay dónde mostrar un mensaje más rico.
 */
export async function downloadFromApi(path: string): Promise<Response> {
  const session = await getSession();
  if (!session) return new Response("Tu sesión terminó. Vuelve a iniciar sesión.", { status: 401 });

  try {
    const response = await apiResponse(path, { token: session.token, signal: AbortSignal.timeout(60_000) });
    const headers = new Headers({ "Cache-Control": "private, no-store", "X-Content-Type-Options": "nosniff" });
    for (const name of PASSTHROUGH) {
      const value = response.headers.get(name);
      if (value) headers.set(name, value);
    }
    // Siempre como descarga: un PDF o una imagen subida por otra persona no se abre dentro del sitio.
    if (!headers.get("content-disposition")?.startsWith("attachment")) headers.set("Content-Disposition", "attachment");
    return new Response(response.body, { status: 200, headers });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    const status = error.status === 404 || error.status === 403 ? 404 : error.status;
    return new Response(status === 404 ? "El archivo no existe o no es tuyo." : error.message, {
      status,
      headers: { "Content-Type": "text/plain; charset=utf-8" },
    });
  }
}
