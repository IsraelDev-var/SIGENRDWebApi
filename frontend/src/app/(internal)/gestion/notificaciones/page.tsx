import type { Metadata } from "next";

import { NotificationList } from "@/features/notifications";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Notificaciones" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

export default async function StaffNotificationsPage({ searchParams }: PageProps<"/gestion/notificaciones">) {
  const session = await requireZone("gestion", "/gestion/notificaciones");
  const params = await searchParams;
  const page = Math.max(1, Math.min(10_000, Number(one(params.pagina)) || 1));
  return <NotificationList session={session} basePath="/gestion/notificaciones" page={page} unreadOnly={one(params.estado) === "sin-leer"} />;
}
