import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { Button } from "@/components/ui/button";
import { formatCount, formatDateTime } from "@/lib/format";
import { cn } from "@/lib/utils";
import { ApiError } from "@/server/api-client";
import type { Session } from "@/server/session";

import { listNotifications } from "../api";
import { MarkReadButton } from "./notification-actions";

type Props = { session: Session; basePath: string; page: number; unreadOnly: boolean };

/** Lista de notificaciones (RF-32): la misma en el portal y en la gestión, cada una con su ruta. */
export async function NotificationList({ session, basePath, page, unreadOnly }: Props) {
  let result: Awaited<ReturnType<typeof listNotifications>> | null = null;
  try {
    result = await listNotifications(session, { page, unreadOnly });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  }

  const href = (n: number, unread = unreadOnly) => `${basePath}?${new URLSearchParams({ ...(unread ? { estado: "sin-leer" } : {}), pagina: String(n) })}`;
  const hasUnread = result?.items.some((n) => !n.isRead) ?? false;

  return (
    <div className="grid max-w-3xl grid-cols-1 gap-6">
      <PageHeader title="Notificaciones" description="Avisos de cambios en tus trámites. También llegan por correo." actions={hasUnread && <MarkReadButton />} />

      <nav aria-label="Filtro" className="flex flex-wrap gap-2">
        {[
          { label: "Todas", unread: false },
          { label: "Sin leer", unread: true },
        ].map((f) => (
          <Link
            key={f.label}
            href={href(1, f.unread)}
            aria-current={unreadOnly === f.unread ? "page" : undefined}
            className={cn(
              "rounded-full border px-3 py-1 text-sm",
              unreadOnly === f.unread ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted",
            )}
          >
            {f.label}
          </Link>
        ))}
      </nav>

      {!result ? (
        <ErrorState title="No se pudieron cargar las notificaciones" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : result.items.length === 0 ? (
        <EmptyState title={unreadOnly ? "No tienes notificaciones sin leer" : "Todavía no tienes notificaciones"} />
      ) : (
        <>
          <p role="status" className="text-sm text-muted-foreground">
            {formatCount(result.totalCount, "notificación", "notificaciones")}.
          </p>
          <ul className="grid divide-y rounded-lg border bg-card">
            {result.items.map((n) => (
              <li key={n.id} className={cn("flex flex-wrap items-start justify-between gap-3 p-4", !n.isRead && "bg-primary/5")}>
                <div className="grid min-w-0 gap-1">
                  <p className="font-medium">
                    {!n.isRead && (
                      <span className="mr-2 inline-block size-2 rounded-full bg-primary align-middle" aria-hidden="true" />
                    )}
                    {n.title}
                    {!n.isRead && <span className="sr-only"> (sin leer)</span>}
                  </p>
                  <p className="text-sm">{n.message}</p>
                  <p className="text-xs text-muted-foreground">
                    <time dateTime={n.createdAt}>{formatDateTime(n.createdAt)}</time>
                  </p>
                </div>
                {!n.isRead && <MarkReadButton id={n.id} title={n.title} />}
              </li>
            ))}
          </ul>
          {result.totalPages > 1 && (
            <nav aria-label="Páginas" className="flex flex-wrap items-center justify-between gap-2">
              {result.page > 1 ? (
                <Button asChild variant="outline" size="sm">
                  <Link href={href(result.page - 1)}>Anterior</Link>
                </Button>
              ) : (
                <span />
              )}
              <span className="text-sm text-muted-foreground">
                Página {result.page} de {result.totalPages}
              </span>
              {result.page < result.totalPages ? (
                <Button asChild variant="outline" size="sm">
                  <Link href={href(result.page + 1)}>Siguiente</Link>
                </Button>
              ) : (
                <span />
              )}
            </nav>
          )}
        </>
      )}
    </div>
  );
}
