import { NextResponse, type NextRequest } from "next/server";

import { canEnterZone, homeFor, zoneOf } from "@/config/permissions";
import { serverEnv } from "@/server/env";
import { verifyAccessToken } from "@/server/token";

/**
 * Comprobación optimista antes de renderizar (solo lee la cookie). La verificación que cuenta está en
 * cada página con requireZone() y, sobre todo, en la API.
 */
export async function proxy(request: NextRequest) {
  const { pathname, search } = request.nextUrl;
  const env = serverEnv();
  const user = await verifyAccessToken(request.cookies.get("em_session")?.value, {
    publicKeyPem: env.ENERGYMAP_JWT_PUBLIC_KEY,
    issuer: env.ENERGYMAP_JWT_ISSUER,
    audience: env.ENERGYMAP_JWT_AUDIENCE,
  });

  const zone = zoneOf(pathname);
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
      return NextResponse.rewrite(new URL("/sin-acceso", request.url), { status: 403 });
    }
    return NextResponse.next();
  }

  // Con sesión, las páginas de ingreso y registro llevan al inicio del rol.
  if (user) return NextResponse.redirect(new URL(homeFor(user.role), request.url));
  return NextResponse.next();
}

export const config = {
  matcher: ["/portal/:path*", "/gestion/:path*", "/ingresar", "/registro"],
};
