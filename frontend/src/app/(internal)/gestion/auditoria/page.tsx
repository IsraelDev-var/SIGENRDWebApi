import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { AUDIT_ACTION_LABEL, AUDIT_ENTITY_LABEL, auditActionLabel, auditEntityLabel, prettyJson, searchAudit } from "@/features/administration";
import { formatCount, formatDateTime } from "@/lib/format";
import { ApiError } from "@/server/api-client";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Auditoría" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);
const short = (value: string | undefined, max: number) => value?.trim().slice(0, max) || undefined;
const isDate = (value: string | undefined) => (value && /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : undefined);

const selectClass =
  "h-9 w-full rounded-md border border-input bg-card px-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring";

/** Registro de auditoría (RN-20, RF-40): quién cambió qué y cuándo, y los eventos de seguridad. Solo el administrador. */
export default async function AuditPage({ searchParams }: PageProps<"/gestion/auditoria">) {
  const session = await requireAction("gestion", "/gestion/auditoria", "viewAudit");
  const params = await searchParams;
  const filters = {
    entity: short(one(params.entidad), 100),
    entityId: short(one(params.id), 100),
    action: short(one(params.accion), 100),
    userId: short(one(params.usuario), 64),
    from: isDate(one(params.desde)),
    to: isDate(one(params.hasta)),
    page: Math.max(1, Math.min(10_000, Number(one(params.pagina)) || 1)),
  };

  let result: Awaited<ReturnType<typeof searchAudit>> | null = null;
  let error: string | null = null;
  try {
    result = await searchAudit(session, filters);
  } catch (e) {
    if (!(e instanceof ApiError)) throw e;
    error = e.errors.join(" ") || e.message;
  }

  const pageHref = (n: number) => {
    const next = new URLSearchParams();
    const keys = { entidad: filters.entity, id: filters.entityId, accion: filters.action, usuario: filters.userId, desde: filters.from, hasta: filters.to };
    for (const [key, value] of Object.entries(keys)) if (value) next.set(key, value);
    next.set("pagina", String(n));
    return `/gestion/auditoria?${next}`;
  };

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader title="Auditoría" description="Cambios en los datos e inicios de sesión, del más reciente al más antiguo. El registro no se puede editar." />

      <form method="get" className="grid gap-3 rounded-lg border bg-card p-4 sm:grid-cols-2 lg:grid-cols-3 lg:items-end">
        <div className="grid gap-1.5">
          <Label htmlFor="a-entidad">Qué</Label>
          <select id="a-entidad" name="entidad" defaultValue={filters.entity ?? ""} className={selectClass}>
            <option value="">Todo</option>
            {Object.entries(AUDIT_ENTITY_LABEL).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="a-accion">Acción</Label>
          <select id="a-accion" name="accion" defaultValue={filters.action ?? ""} className={selectClass}>
            <option value="">Todas</option>
            {Object.entries(AUDIT_ACTION_LABEL).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="a-id">Identificador del registro</Label>
          <Input id="a-id" name="id" defaultValue={filters.entityId} maxLength={100} />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="a-usuario">Usuario (identificador)</Label>
          <Input id="a-usuario" name="usuario" defaultValue={filters.userId} maxLength={64} />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="a-desde">Desde</Label>
          <Input id="a-desde" name="desde" type="date" defaultValue={filters.from} />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="a-hasta">Hasta</Label>
          <Input id="a-hasta" name="hasta" type="date" defaultValue={filters.to} />
        </div>
        <div className="flex gap-2 lg:col-span-3">
          <Button type="submit">Filtrar</Button>
          <Button asChild variant="ghost">
            <Link href="/gestion/auditoria">Quitar filtros</Link>
          </Button>
        </div>
      </form>

      {error !== null ? (
        <ErrorState title="No se pudo consultar la auditoría" description={error} />
      ) : !result || result.items.length === 0 ? (
        <EmptyState title="No hay eventos con esos filtros" />
      ) : (
        <>
          <p role="status" className="text-sm text-muted-foreground">
            {formatCount(result.totalCount, "evento", "eventos")}. Página {result.page} de {Math.max(1, result.totalPages)}.
          </p>
          <div className="rounded-lg border bg-card">
            <Table label="Registro de auditoría">
              <TableHeader>
                <TableRow>
                  <TableHead scope="col">Fecha</TableHead>
                  <TableHead scope="col">Acción</TableHead>
                  <TableHead scope="col">Qué</TableHead>
                  <TableHead scope="col">Usuario</TableHead>
                  <TableHead scope="col">Detalle</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {result.items.map((e) => {
                  const before = prettyJson(e.oldValues);
                  const after = prettyJson(e.newValues);
                  return (
                    <TableRow key={e.id} className="align-top">
                      <TableCell>
                        <time dateTime={e.createdAt}>{formatDateTime(e.createdAt)}</time>
                      </TableCell>
                      <TableCell>{auditActionLabel(e.action)}</TableCell>
                      <TableCell>
                        {auditEntityLabel(e.entityName)}
                        {e.entityId && <span className="block font-mono text-xs text-muted-foreground">{e.entityId}</span>}
                      </TableCell>
                      <TableCell>
                        {e.userId ? (
                          <Link
                            href={`/gestion/auditoria?usuario=${encodeURIComponent(e.userId)}`}
                            className="font-mono text-xs text-link underline underline-offset-4 hover:no-underline"
                            title={e.userId}
                          >
                            {e.userId.slice(0, 8)}…<span className="sr-only"> ver todo lo de este usuario</span>
                          </Link>
                        ) : (
                          <span className="text-muted-foreground">Sistema o anónimo</span>
                        )}
                      </TableCell>
                      <TableCell className="whitespace-normal">
                        {before || after ? (
                          <details>
                            <summary className="cursor-pointer text-sm">Ver valores</summary>
                            <div className="mt-2 grid gap-2">
                              {before && (
                                <div>
                                  <p className="text-xs text-muted-foreground">Antes</p>
                                  <pre className="max-h-48 max-w-md overflow-auto rounded bg-muted p-2 text-xs">{before}</pre>
                                </div>
                              )}
                              {after && (
                                <div>
                                  <p className="text-xs text-muted-foreground">Después</p>
                                  <pre className="max-h-48 max-w-md overflow-auto rounded bg-muted p-2 text-xs">{after}</pre>
                                </div>
                              )}
                            </div>
                          </details>
                        ) : (
                          <span className="text-muted-foreground">Sin detalle</span>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
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
