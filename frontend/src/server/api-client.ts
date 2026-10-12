import "server-only";

import { serverEnv } from "@/server/env";

/**
 * Llamadas a la API .NET desde el servidor de Next. Desenvuelve el sobre Response<T> y convierte
 * los errores en ApiError con el mensaje que la API pensó para el usuario.
 */

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    /** Errores de validación de FluentValidation, si los hay. */
    public readonly errors: string[] = [],
  ) {
    super(message);
    this.name = "ApiError";
  }
}

type Options = {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  /** JWT de la sesión; sin él la llamada es anónima. */
  token?: string;
  signal?: AbortSignal;
};

const GENERIC_ERROR = "No pudimos comunicarnos con el servidor. Inténtalo de nuevo en unos minutos.";

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

/** Respuesta cruda de la API (sin desenvolver), para endpoints que no usan Response<T>, como /Auth. */
export async function apiRequest<T>(path: string, { method = "GET", body, token, signal }: Options = {}): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${serverEnv().ENERGYMAP_API_URL.replace(/\/$/, "")}/api/v1${path}`, {
      method,
      headers: {
        Accept: "application/json",
        ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
      cache: "no-store",
      signal: signal ?? AbortSignal.timeout(15_000),
    });
  } catch {
    throw new ApiError(503, GENERIC_ERROR);
  }

  const json = (await readJson(response)) as { message?: unknown; errors?: unknown } | null;

  if (!response.ok) {
    const message = typeof json?.message === "string" && json.message ? json.message : GENERIC_ERROR;
    const errors = Array.isArray(json?.errors) ? json.errors.filter((e): e is string => typeof e === "string") : [];
    throw new ApiError(response.status, response.status >= 500 ? GENERIC_ERROR : message, errors);
  }

  return json as T;
}

/** Endpoints que responden Response<T>: devuelve `data`. */
export async function apiData<T>(path: string, options?: Options): Promise<T> {
  const envelope = await apiRequest<{ succeeded?: boolean; data?: T; message?: string }>(path, options);
  if (!envelope || envelope.succeeded === false) {
    throw new ApiError(400, envelope?.message ?? GENERIC_ERROR);
  }
  return envelope.data as T;
}
