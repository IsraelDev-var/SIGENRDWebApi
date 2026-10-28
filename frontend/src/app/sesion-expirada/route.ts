import { safeRedirectPath } from "@/lib/safe-redirect";
import { deleteSession } from "@/server/session";

/**
 * La sesión ya no sirve (venció o un administrador desactivó la cuenta): se borra la cookie y se vuelve a ingresar con
 * un aviso, y después a la página donde estaba (?next=, solo rutas internas). Lo usan api-client.ts al recibir un 401 con
 * token y el aviso de vencimiento.
 */
export async function GET(request: Request) {
  await deleteSession();
  const url = new URL(request.url);
  const login = new URL("/ingresar", url);
  login.searchParams.set("motivo", "expirada");
  const next = safeRedirectPath(url.searchParams.get("next"), "");
  if (next) login.searchParams.set("next", next);
  return Response.redirect(login, 303);
}
