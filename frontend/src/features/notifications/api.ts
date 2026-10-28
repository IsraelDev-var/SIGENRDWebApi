import "server-only";

import type { components } from "@/lib/api/schema";
import { apiData } from "@/server/api-client";
import type { Session } from "@/server/session";

export type NotificationPage = components["schemas"]["NotificationDtoPagedDto"];
export const PAGE_SIZE = 20;

/** Notificaciones del usuario, sea cual sea su rol (RF-32), de la más reciente a la más antigua. */
export function listNotifications(session: Session, { page, unreadOnly }: { page: number; unreadOnly: boolean }) {
  const query = new URLSearchParams({ Page: String(page), PageSize: String(PAGE_SIZE), UnreadOnly: String(unreadOnly) });
  return apiData<NotificationPage>(`/Notifications?${query}`, { token: session.token });
}

export function unreadCount(session: Session) {
  return apiData<number>("/Notifications/unread-count", { token: session.token });
}
