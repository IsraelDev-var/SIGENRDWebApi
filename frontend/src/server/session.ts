import "server-only";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { cache } from "react";

import { canEnterZone, type Zone } from "@/config/permissions";
import { serverEnv } from "@/server/env";
import { verifyAccessToken, type SessionUser } from "@/server/token";

/**
 * Sesión del usuario (BFF): el JWT de la API vive en una cookie HttpOnly y nunca llega al navegador.
 * Esta es la verificación real; proxy.ts solo hace la comprobación optimista.
 */

export const SESSION_COOKIE = "em_session";

export type Session = SessionUser & { token: string };

export function tokenSettings() {
  const env = serverEnv();
  return { publicKeyPem: env.ENERGYMAP_JWT_PUBLIC_KEY, issuer: env.ENERGYMAP_JWT_ISSUER, audience: env.ENERGYMAP_JWT_AUDIENCE };
}

/** Sesión válida de la petición actual, o null. Se calcula una vez por petición. */
export const getSession = cache(async (): Promise<Session | null> => {
  const token = (await cookies()).get(SESSION_COOKIE)?.value;
  const user = await verifyAccessToken(token, tokenSettings());
  return user && token ? { ...user, token } : null;
});

/** Guarda el token recién emitido. Devuelve el usuario, o null si la API entregó un token que no se puede verificar. */
export async function createSession(token: string): Promise<SessionUser | null> {
  const user = await verifyAccessToken(token, tokenSettings());
  if (!user) return null;

  (await cookies()).set(SESSION_COOKIE, token, {
    httpOnly: true,
    // Los navegadores tratan localhost como origen seguro, así que también funciona en desarrollo.
    secure: true,
    sameSite: "lax",
    path: "/",
    expires: new Date(user.expiresAt),
  });
  return user;
}

export async function deleteSession(): Promise<void> {
  (await cookies()).delete(SESSION_COOKIE);
}

/**
 * Para cada página privada: sin sesión lleva a /ingresar (y vuelve después); con un rol que no
 * corresponde a la zona, a /sin-acceso.
 */
export async function requireZone(zone: Zone, currentPath: string): Promise<Session> {
  const session = await getSession();
  if (!session) redirect(`/ingresar?next=${encodeURIComponent(currentPath)}`);
  if (!canEnterZone(session.role, zone)) redirect("/sin-acceso");
  return session;
}
