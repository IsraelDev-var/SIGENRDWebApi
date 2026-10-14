import type { Metadata } from "next";
import Link from "next/link";

import { KvaValue } from "@/components/shared/kva-value";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { RequestStatusBadge } from "@/components/shared/request-status-badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { REQUEST_STATUS, REQUEST_TYPE_LABEL, type RequestStatus } from "@/config/status";
import { listRequests, QUEUE_STATUSES } from "@/features/management";
import { daysBetween, formatCount, formatDate } from "@/lib/format";
import { ApiError } from "@/server/api-client";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Bandeja de solicitudes" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);
const isDate = (value: string | undefined) => (value && /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : undefined);

const selectClass =
  "h-9 w-full rounded-md border border-input bg-card px-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring";

/** Bandeja de revisión (RF-12, RF-14): filtros por GET, en la URL, para compartir o volver con Atrás. */
export default async function StaffRequestsPage({ searchParams }: PageProps<"/gestion/solicitudes">) {
  const session = await requireAction("gestion", "/gestion/solicitudes", "viewOthersRequests");
  const params = await searchParams;

  const status = (QUEUE_STATUSES as readonly string[]).includes(one(params.estado) ?? "") ? one(params.estado) : undefined;
  const type = ["Connection", "NetMetering"].includes(one(params.tipo) ?? "") ? one(params.tipo) : undefined;
  const text = one(params.texto)?.trim().slice(0, 100) || undefined;
  const from = isDate(one(params.desde));
  const to = isDate(one(params.hasta));
  const page = Math.max(1, Math.min(10_000, Number(one(params.pagina)) || 1));

  let result: Awaited<ReturnType<typeof listRequests>> | null = null;
  let error: string | null = null;
  try {
    result = await listRequests(session, { status, type, text, from, to, page });
  } catch (e) {
    if (!(e instanceof ApiError)) throw e;
    error = e.errors.join(" ") || e.message;
  }

  const pageHref = (n: number) => {
    const next = new URLSearchParams();
    if (status) next.set("estado", status);
    if (type) next.set("tipo", type);
    if (text) next.set("texto", text);
    if (from) next.set("desde", from);
    if (to) next.set("hasta", to);
    next.set("pagina", String(n));
    return `/gestion/solicitudes?${next}`;
  };
  const now = new Date();

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader title="Solicitudes" description="Todas las solicitudes enviadas a la distribuidora. Los borradores no aparecen hasta que el solicitante los envía." />

      <form method="get" className="grid gap-3 rounded-lg border bg-card p-4 sm:grid-cols-2 lg:grid-cols-6 lg:items-end">
        <div className="grid gap-1.5">
          <Label htmlFor="f-estado">Estado</Label>
          <select id="f-estado" name="estado" defaultValue={status ?? ""} className={selectClass}>
            <option value="">Todos</option>
            {QUEUE_STATUSES.map((s) => (
              <option key={s} value={s}>
                {REQUEST_STATUS[s as RequestStatus].label}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="f-tipo">Trámite</Label>
          <select id="f-tipo" name="tipo" defaultValue={type ?? ""} className={selectClass}>
            <option value="">Todos</option>
            <option value="Connection">{REQUEST_TYPE_LABEL.Connection}</option>
            <option value="NetMetering">{REQUEST_TYPE_LABEL.NetMetering}</option>
          </select>
        </div>
        <div className="grid gap-1.5 lg:col-span-2">
          <Label htmlFor="f-texto">Expediente o dirección</Label>
          <Input id="f-texto" name="texto" defaultValue={text} maxLength={100} />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="f-desde">Desde</Label>
          <Input id="f-desde" name="desde" type="date" defaultValue={from} />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="f-hasta">Hasta</Label>
          <Input id="f-hasta" name="hasta" type="date" defaultValue={to} />
        </div>
        <div className="flex gap-2 lg:col-span-6">
          <Button type="submit">Filtrar</Button>
          <Button asChild variant="ghost">
            <Link href="/gestion/solicitudes">Quitar filtros</Link>
          </Button>
        </div>
      </form>

      {error !== null ? (
        <ErrorState title="No se pudo cargar la bandeja" description={error} />
      ) : !result || result.items.length === 0 ? (
        <EmptyState title="No hay solicitudes con esos filtros" description="Prueba con otro estado o quita algún filtro." />
      ) : (
        <>
          <p role="status" className="text-sm text-muted-foreground">
            {formatCount(result.totalCount, "solicitud", "solicitudes")}. Página {result.page} de {Math.max(1, result.totalPages)}.
          </p>
          <div className="rounded-lg border bg-card">
            <Table label="Bandeja de solicitudes">
              <TableHeader>
                <TableRow>
                  <TableHead scope="col">Expediente</TableHead>
                  <TableHead scope="col">Titular</TableHead>
                  <TableHead scope="col">Trámite</TableHead>
                  <TableHead scope="col" className="text-right">
                    Capacidad
                  </TableHead>
                  <TableHead scope="col">Transformador</TableHead>
                  <TableHead scope="col">Estado</TableHead>
                  <TableHead scope="col">Recibida</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {result.items.map((r) => (
                  <TableRow key={r.id}>
                    <TableCell>
                      <Link href={`/gestion/solicitudes/${r.id}`} className="font-mono text-link underline underline-offset-4 hover:no-underline">
                        {r.requestCode}
                      </Link>
                    </TableCell>
                    <TableCell>
                      {r.holderName ?? "Sin dato"}
                      {r.installerCompanyName && <span className="block text-xs text-muted-foreground">vía {r.installerCompanyName}</span>}
                    </TableCell>
                    <TableCell>{REQUEST_TYPE_LABEL[r.requestType as keyof typeof REQUEST_TYPE_LABEL] ?? r.requestType}</TableCell>
                    <TableCell className="text-right">
                      <KvaValue value={r.requestedCapacityKva} />
                    </TableCell>
                    <TableCell className="font-mono">{r.transformerCode ?? "Sin dato"}</TableCell>
                    <TableCell>
                      <RequestStatusBadge status={r.status} />
                    </TableCell>
                    <TableCell>
                      {formatDate(r.requestedAt)}
                      <span className="block text-xs text-muted-foreground">hace {daysBetween(r.requestedAt, now)} días</span>
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
