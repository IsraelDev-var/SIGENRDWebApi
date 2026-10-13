import type { Metadata } from "next";
import Link from "next/link";

import { KvaValue } from "@/components/shared/kva-value";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { RequestStatusBadge } from "@/components/shared/request-status-badge";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { asRequestStatus, REQUEST_STATUS, REQUEST_TYPE_LABEL } from "@/config/status";
import { listMyRequests } from "@/features/requests";
import { formatDate } from "@/lib/format";
import { ApiError } from "@/server/api-client";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Mis solicitudes" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

const selectClass =
  "h-9 w-full rounded-md border border-input bg-card px-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring";

/** Solicitudes del usuario, con sus borradores (RF-13, RN-22). Filtro y páginas por GET: funcionan sin JavaScript. */
export default async function MyRequestsPage({ searchParams }: PageProps<"/portal/solicitudes">) {
  const session = await requireZone("portal", "/portal/solicitudes");
  const params = await searchParams;
  const status = asRequestStatus(one(params.estado));
  const page = Math.max(1, Math.min(10_000, Number(one(params.pagina)) || 1));
  const isInstaller = session.role === "InstallerCompany";

  let result: Awaited<ReturnType<typeof listMyRequests>> | null = null;
  try {
    result = await listMyRequests(session, { page, status: status ?? undefined });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  }

  const pageHref = (n: number) => {
    const next = new URLSearchParams();
    if (status) next.set("estado", status);
    next.set("pagina", String(n));
    return `/portal/solicitudes?${next}`;
  };

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader
        title="Mis solicitudes"
        description={
          isInstaller
            ? "Las solicitudes que tu empresa creó a nombre de sus clientes."
            : "Tus solicitudes de conexión y de medición neta, incluidos los borradores sin enviar."
        }
        actions={
          <Button asChild>
            <Link href="/portal/solicitudes/nueva">Nueva solicitud</Link>
          </Button>
        }
      />

      <form method="get" className="flex flex-wrap items-end gap-3">
        <div className="grid min-w-48 gap-1.5">
          <Label htmlFor="filtro-estado">Estado</Label>
          <select id="filtro-estado" name="estado" defaultValue={status ?? ""} className={selectClass}>
            <option value="">Todos</option>
            {Object.entries(REQUEST_STATUS).map(([value, { label }]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>
        <Button type="submit" variant="outline">
          Filtrar
        </Button>
      </form>

      {!result ? (
        <ErrorState title="No se pudieron cargar tus solicitudes" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : result.items.length === 0 ? (
        <EmptyState
          title={status ? "No tienes solicitudes en ese estado" : "Todavía no tienes solicitudes"}
          description={status ? "Prueba con otro estado." : "Elige un transformador en el mapa para empezar una solicitud."}
          action={
            !status && (
              <Button asChild>
                <Link href="/portal/solicitudes/nueva">Nueva solicitud</Link>
              </Button>
            )
          }
        />
      ) : (
        <>
          <p role="status" className="text-sm text-muted-foreground">
            {result.totalCount.toLocaleString("es-DO")} solicitudes. Página {result.page} de {Math.max(1, result.totalPages)}.
          </p>
          <div className="rounded-lg border bg-card">
            <Table label="Mis solicitudes">
              <TableHeader>
                <TableRow>
                  <TableHead scope="col">Expediente</TableHead>
                  {isInstaller && <TableHead scope="col">Titular</TableHead>}
                  <TableHead scope="col">Trámite</TableHead>
                  <TableHead scope="col">Transformador</TableHead>
                  <TableHead scope="col" className="text-right">
                    Capacidad
                  </TableHead>
                  <TableHead scope="col">Estado</TableHead>
                  <TableHead scope="col">Fecha</TableHead>
                  <TableHead scope="col">
                    <span className="sr-only">Acciones</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {result.items.map((r) => (
                  <TableRow key={r.id}>
                    <TableCell className="font-mono">{r.requestCode}</TableCell>
                    {isInstaller && <TableCell>{r.holderName ?? "Sin dato"}</TableCell>}
                    <TableCell>{REQUEST_TYPE_LABEL[r.requestType as keyof typeof REQUEST_TYPE_LABEL] ?? r.requestType}</TableCell>
                    <TableCell className="font-mono">{r.transformerCode ?? "Sin dato"}</TableCell>
                    <TableCell className="text-right">
                      <KvaValue value={r.requestedCapacityKva} />
                    </TableCell>
                    <TableCell>
                      <RequestStatusBadge status={r.status} />
                    </TableCell>
                    <TableCell>{formatDate(r.requestedAt)}</TableCell>
                    <TableCell className="text-right">
                      <Link href={`/portal/solicitudes/${r.id}`} className="text-sm text-link underline underline-offset-4 hover:no-underline">
                        {r.status === "Draft" ? "Continuar" : "Ver expediente"}
                        <span className="sr-only"> {r.requestCode}</span>
                      </Link>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          {result.totalPages > 1 && (
            <nav aria-label="Páginas" className="flex flex-wrap items-center justify-between gap-2">
              {result.page > 1 ? (
                <Button asChild variant="outline" size="sm">
                  <Link href={pageHref(result.page - 1)}>Anterior</Link>
                </Button>
              ) : (
                <span />
              )}
              <span className="text-sm text-muted-foreground">
                Página {result.page} de {result.totalPages}
              </span>
              {result.page < result.totalPages ? (
                <Button asChild variant="outline" size="sm">
                  <Link href={pageHref(result.page + 1)}>Siguiente</Link>
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
