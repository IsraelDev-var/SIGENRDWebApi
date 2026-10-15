import { unreadCount } from "@/features/notifications";
import { getSession } from "@/server/session";

/** Notificaciones sin leer para la campana (BFF: el token queda en el servidor). Sin sesión, 0. */
export async function GET() {
  const session = await getSession();
  if (!session) return Response.json({ count: 0 }, { status: 401, headers: { "Cache-Control": "no-store" } });
  const count = await unreadCount(session);
  return Response.json({ count }, { headers: { "Cache-Control": "private, no-store" } });
}
