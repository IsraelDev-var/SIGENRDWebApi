import "server-only";

import { headers } from "next/headers";

import { clientIpFrom } from "@/server/client-ip";
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
  /** X-Forwarded-For de la petición del usuario; si no se pasa, se lee de la petición en curso. */
  forwardedFor?: string | null;
};

/** Cabeceras para que la API limite por la IP del usuario y no por la de este servidor (TrustedProxyClientIpMiddleware). */
export async function clientIpHeaders(forwardedFor?: string | null): Promise<Record<string, string>> {
  const env = serverEnv();
  if (!env.ENERGYMAP_PROXY_KEY) return {};

  let value = forwardedFor;
  if (value === undefined) {
    try {
      value = (await headers()).get("x-forwarded-for");
    } catch {
      // Fuera de una petición (por ejemplo, al compilar): no hay usuario.
      value = null;
    }
  }

  const ip = clientIpFrom(value, env.ENERGYMAP_TRUSTED_PROXIES);
  return ip ? { "X-EnergyMap-Proxy-Key": env.ENERGYMAP_PROXY_KEY, "X-EnergyMap-Client-IP": ip } : {};
}

const GENERIC_ERROR = "No pudimos comunicarnos con el servidor. Inténtalo de nuevo en unos minutos.";
// El límite de peticiones de la API responde 429 sin mensaje.
const TOO_MANY_REQUESTS = "Hiciste muchos intentos seguidos. Espera un minuto y vuelve a intentarlo.";

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
export async function apiRequest<T>(path: string, { method = "GET", body, token, signal, forwardedFor }: Options = {}): Promise<T> {
  const forwarded = await clientIpHeaders(forwardedFor);
  let response: Response;
  try {
    response = await fetch(`${serverEnv().ENERGYMAP_API_URL.replace(/\/$/, "")}/api/v1${path}`, {
      method,
      headers: {
        Accept: "application/json",
        ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...forwarded,
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
    const fallback = response.status === 429 ? TOO_MANY_REQUESTS : GENERIC_ERROR;
    const message = typeof json?.message === "string" && json.message ? json.message : fallback;
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
