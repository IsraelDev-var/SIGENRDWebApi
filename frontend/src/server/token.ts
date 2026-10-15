import { importSPKI, jwtVerify, type CryptoKey, type JWTPayload } from "jose";

import { isRole, type Role } from "@/config/permissions";

/**
 * Verificación del JWT que emite la API (RS256, RNF-06). No depende de next/headers para poder usarse
 * también en proxy.ts y en las pruebas.
 */

export type SessionUser = {
  userId: string;
  email: string;
  fullName: string;
  role: Role;
  /** Expiración del token, en milisegundos desde 1970. */
  expiresAt: number;
  /** Contraseña temporal puesta por un administrador: la API solo deja cambiarla (claim pwd_change). */
  mustChangePassword: boolean;
};

export type TokenSettings = {
  publicKeyPem: string;
  issuer: string;
  audience: string;
};

// La API usa ClaimTypes.Role; según el serializador llega como "role" o con el URI completo.
const ROLE_CLAIMS = ["role", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

const keyCache = new Map<string, Promise<CryptoKey>>();

function publicKey(pem: string): Promise<CryptoKey> {
  let key = keyCache.get(pem);
  if (!key) {
    key = importSPKI(pem, "RS256");
    keyCache.set(pem, key);
  }
  return key;
}

function roleFrom(payload: JWTPayload): Role | null {
  for (const claim of ROLE_CLAIMS) {
    const value = payload[claim];
    const values = Array.isArray(value) ? value : [value];
    const role = values.find(isRole);
    if (role) return role;
  }
  return null;
}

/** El usuario del token, o null si la firma, el emisor, la audiencia, la vigencia o el rol no son válidos. */
export async function verifyAccessToken(token: string | undefined, settings: TokenSettings): Promise<SessionUser | null> {
  if (!token) return null;

  try {
    const { payload } = await jwtVerify(token, await publicKey(settings.publicKeyPem), {
      algorithms: ["RS256"],
      issuer: settings.issuer,
      audience: settings.audience,
    });

    const role = roleFrom(payload);
    if (!role || typeof payload.sub !== "string" || typeof payload.exp !== "number") return null;

    return {
      userId: payload.sub,
      email: typeof payload.email === "string" ? payload.email : "",
      fullName: typeof payload.FullName === "string" ? payload.FullName : "",
      role,
      expiresAt: payload.exp * 1000,
      mustChangePassword: payload.pwd_change === "true",
    };
  } catch {
    return null;
  }
}
