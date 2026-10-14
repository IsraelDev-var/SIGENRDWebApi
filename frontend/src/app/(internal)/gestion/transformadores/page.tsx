import type { Metadata } from "next";
import Link from "next/link";

import { AvailabilityValue } from "@/components/shared/availability-value";
import { KvaValue } from "@/components/shared/kva-value";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { TransformerStatusBadge } from "@/components/shared/transformer-status-badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { asTransformerStatus, TRANSFORMER_STATUS, TRANSFORMER_STATUSES } from "@/config/status";
import { listTransformers, listZones } from "@/features/management";
import { formatCount, formatDate } from "@/lib/format";
import { ApiError } from "@/server/api-client";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Transformadores" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

const SORTS = [
  { value: "Code", label: "Código" },
  { value: "Availability", label: "Menos capacidad libre" },
  { value: "LastUpdated", label: "Dato más antiguo" },
];

const selectClass =
  "h-9 w-full rounded-md border border-input bg-card px-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring";

/** Gestión de la red (B-3): buscar, filtrar y ordenar para encontrar lo que hay que actualizar. */
export default async function ManageTransformersPage({ searchParams }: PageProps<"/gestion/transformadores">) {
  const session = await requireAction("gestion", "/gestion/transformadores", "manageCatalogs");
  const params = await searchParams;

  const code = one(params.codigo)?.trim().slice(0, 50) || undefined;
  const zone = /^\d{1,6}$/.test(one(params.zona) ?? "") ? one(params.zona) : undefined;
  const status = asTransformerStatus(one(params.estado)) ?? undefined;
  const sort = SORTS.some((s) => s.value === one(params.orden)) ? one(params.orden) : undefined;
  const page = Math.max(1, Math.min(10_000, Number(one(params.pagina)) || 1));

  let result: Awaited<ReturnType<typeof listTransformers>> | null = null;
  let zones: Awaited<ReturnType<typeof listZones>> = [];
  try {
    [result, zones] = await Promise.all([listTransformers(session, { code, zone, status, sort, page }), listZones()]);
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  }

  const pageHref = (n: number) => {
    const next = new URLSearchParams();
    if (code) next.set("codigo", code);
    if (zone) next.set("zona", zone);
    if (status) next.set("estado", status);
    if (sort) next.set("orden", sort);
    next.set("pagina", String(n));
    return `/gestion/transformadores?${next}`;
  };

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader
        title="Transformadores"
        description="Capacidad informada por la distribuidora. El estado y el porcentaje los calcula el sistema (RN-01, RN-02)."
        actions={
          <>
            <Button asChild>
              <Link href="/gestion/transformadores/nuevo">Nuevo transformador</Link>
            </Button>
            <Button asChild variant="outline">
              <Link href="/gestion/transformadores/importar">Importar CSV</Link>
            </Button>
          </>
        }
      />

      <form method="get" className="grid gap-3 rounded-lg border bg-card p-4 sm:grid-cols-2 lg:grid-cols-5 lg:items-end">
        <div className="grid gap-1.5">
          <Label htmlFor="t-codigo">Código</Label>
          <Input id="t-codigo" name="codigo" defaultValue={code} maxLength={50} />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="t-zona">Zona</Label>
          <select id="t-zona" name="zona" defaultValue={zone ?? ""} className={selectClass}>
            <option value="">Todas</option>
            {zones.map((z) => (
              <option key={z.id} value={z.id}>
                {z.zoneName}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="t-estado">Estado</Label>
          <select id="t-estado" name="estado" defaultValue={status ?? ""} className={selectClass}>
            <option value="">Todos</option>
            {TRANSFORMER_STATUSES.map((s) => (
              <option key={s} value={s}>
                {TRANSFORMER_STATUS[s].label}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="t-orden">Ordenar por</Label>
          <select id="t-orden" name="orden" defaultValue={sort ?? "Code"} className={selectClass}>
            {SORTS.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </div>
        <Button type="submit">Filtrar</Button>
      </form>

      {!result ? (
        <ErrorState title="No se pudo cargar la red" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : result.items.length === 0 ? (
        <EmptyState title="No hay transformadores con esos filtros" description="Prueba con otro código o quita algún filtro." />
      ) : (
        <>
          <p role="status" className="text-sm text-muted-foreground">
            {formatCount(result.totalCount, "transformador", "transformadores")}. Página {result.page} de {Math.max(1, result.totalPages)}.
          </p>
          <div className="rounded-lg border bg-card">
            <Table label="Red de transformadores">
              <TableHeader>
                <TableRow>
                  <TableHead scope="col">Código</TableHead>
                  <TableHead scope="col">Zona</TableHead>
                  <TableHead scope="col">Estado</TableHead>
                  <TableHead scope="col" className="text-right">
                    Total
                  </TableHead>
                  <TableHead scope="col" className="text-right">
                    Libre
                  </TableHead>
                  <TableHead scope="col">Actualizado</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {result.items.map((t) => (
                  <TableRow key={t.id}>
                    <TableCell>
                      <Link href={`/gestion/transformadores/${t.id}`} className="font-mono text-link underline underline-offset-4 hover:no-underline">
                        {t.code}
                      </Link>
                    </TableCell>
                    <TableCell>{t.zoneName ?? "Sin zona"}</TableCell>
                    <TableCell>
                      <TransformerStatusBadge status={t.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <KvaValue value={t.totalCapacityKva} />
                    </TableCell>
                    <TableCell className="text-right">
                      <AvailabilityValue pct={t.availabilityPct} status={t.status} />
                    </TableCell>
                    <TableCell>{formatDate(t.lastUpdated)}</TableCell>
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
