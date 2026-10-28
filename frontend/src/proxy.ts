import { NextResponse, type NextRequest } from "next/server";

import { MAP_STYLES } from "@/config/map";
import { canEnterZone, homeFor, zoneOf } from "@/config/permissions";
import { contentSecurityPolicy, newNonce } from "@/config/security";
import { publicApiOrigin, serverEnv } from "@/server/env";
import { verifyAccessToken } from "@/server/token";

const AUTH_PAGES = new Set(["/ingresar", "/registro"]);

/**
 * Corre antes de renderizar cada página:
 * 1. Content Security Policy con un nonce nuevo (F9). Next lo lee de la cabecera de la petición y lo pone en sus scripts.
 * 2. Comprobación optimista de la sesión en las zonas privadas y en ingresar/registro (solo lee la cookie). La
 *    verificación que cuenta está en cada página con requireZone() y, sobre todo, en la API.
 */
export async function proxy(request: NextRequest) {
  const { pathname, search } = request.nextUrl;
  const zone = zoneOf(pathname);

  if (zone || AUTH_PAGES.has(pathname)) {
    const env = serverEnv();
    const user = await verifyAccessToken(request.cookies.get("em_session")?.value, {
      publicKeyPem: env.ENERGYMAP_JWT_PUBLIC_KEY,
      issuer: env.ENERGYMAP_JWT_ISSUER,
      audience: env.ENERGYMAP_JWT_AUDIENCE,
    });

    if (zone) {
      if (!user) {
        const login = new URL("/ingresar", request.url);
        login.searchParams.set("next", pathname + search);
        return NextResponse.redirect(login);
      }
      // Contraseña temporal: la API no responde a nada más hasta cambiarla.
      if (user.mustChangePassword) return NextResponse.redirect(new URL("/cambiar-contrasena", request.url));
      if (!canEnterZone(user.role, zone)) {
        // Se queda la URL pedida y se responde 403 con la página de acceso denegado.
        return withCsp(request, (headers) => NextResponse.rewrite(new URL("/sin-acceso", request.url), { status: 403, request: { headers } }));
      }
    } else if (user) {
      // Con sesión, las páginas de ingreso y registro llevan al inicio del rol.
      return NextResponse.redirect(new URL(homeFor(user.role), request.url));
    }
  }

  return withCsp(request, (headers) => NextResponse.next({ request: { headers } }));
}

function withCsp(request: NextRequest, respond: (headers: Headers) => NextResponse): NextResponse {
  const nonce = newNonce();
  const policy = contentSecurityPolicy(nonce, {
    dev: process.env.NODE_ENV === "development",
    https: request.nextUrl.protocol === "https:" || request.headers.get("x-forwarded-proto") === "https",
    mapStyles: [MAP_STYLES.light, MAP_STYLES.dark],
    apiOrigin: publicApiOrigin(),
  });

  const headers = new Headers(request.headers);
  headers.set("x-nonce", nonce);
  headers.set("Content-Security-Policy", policy);

  const response = respond(headers);
  response.headers.set("Content-Security-Policy", policy);
  return response;
}

export const config = {
  matcher: [
    // Todas las páginas. Fuera: los route handlers de /api (JSON), los archivos de Next y los estáticos de public/.
    // Tampoco las precargas de next/link: no renderizan HTML propio.
    {
      source: "/((?!api|_next/static|_next/image|favicon.ico|vendor/|.*\\.(?:csv|svg|png|jpg|ico|webmanifest)$).*)",
      missing: [
        { type: "header", key: "next-router-prefetch" },
        { type: "header", key: "purpose", value: "prefetch" },
      ],
    },
  ],
};
