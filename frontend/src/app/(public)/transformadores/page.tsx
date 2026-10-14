import type { Metadata } from "next";
import Link from "next/link";

import { AvailabilityValue } from "@/components/shared/availability-value";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { TransformerStatusBadge } from "@/components/shared/transformer-status-badge";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { DOMINICAN_REPUBLIC_BOUNDS } from "@/config/map";
import { asTransformerStatus, TRANSFORMER_STATUS, TRANSFORMER_STATUSES } from "@/config/status";
import { MapLegend } from "@/features/map";
import type { components } from "@/lib/api/schema";
import { formatCount } from "@/lib/format";
import { ApiError, apiData } from "@/server/api-client";

export const metadata: Metadata = {
  title: "Transformadores",
  description: "Lista de transformadores de distribución con su disponibilidad, para consultar sin el mapa.",
};

type MapResult = components["schemas"]["TransformerMapResultDto"];
type Zone = components["schemas"]["ServiceZoneDto"];

const PAGE_SIZE = 50;
const MAX_ROWS = 5000;

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

const selectClass =
  "h-9 w-full rounded-md border border-input bg-card px-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring";

/**
 * Alternativa al mapa (WCAG, RNF-15): los mismos datos y filtros en una tabla que funciona con teclado, lector de
 * pantalla y sin JavaScript (el formulario es un GET normal).
 */
export default async function TransformersListPage({ searchParams }: PageProps<"/transformadores">) {
  const params = await searchParams;
  const status = asTransformerStatus(one(params.estado));
  const zoneId = Number(one(params.zona)) || null;
  const minAvailability = [30, 70].includes(Number(one(params.disp))) ? Number(one(params.disp)) : null;
  const page = Math.max(1, Number(one(params.pagina)) || 1);

  const [[minLng, minLat], [maxLng, maxLat]] = DOMINICAN_REPUBLIC_BOUNDS;
  const query = new URLSearchParams({
    minLatitude: String(minLat),
    minLongitude: String(minLng),
    maxLatitude: String(maxLat),
    maxLongitude: String(maxLng),
    limit: String(MAX_ROWS),
  });
  if (status) query.set("status", status);
  if (zoneId) query.set("serviceZoneId", String(zoneId));
  if (minAvailability) query.set("minAvailabilityPct", String(minAvailability));

  let result: MapResult | null = null;
  let zones: Zone[] = [];
  let failed = false;
  try {
    [result, zones] = await Promise.all([apiData<MapResult>(`/Transformers?${query}`), apiData<Zone[]>("/ServiceZones")]);
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    failed = true;
  }

  const rows = [...(result?.items ?? [])].sort((a, b) => (a.code ?? "").localeCompare(b.code ?? ""));
  const pages = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
  const current = Math.min(page, pages);
  const visible = rows.slice((current - 1) * PAGE_SIZE, current * PAGE_SIZE);

  const pageHref = (n: number) => {
    const next = new URLSearchParams();
    if (status) next.set("estado", status);
    if (zoneId) next.set("zona", String(zoneId));
    if (minAvailability) next.set("disp", String(minAvailability));
    next.set("pagina", String(n));
    return `/transformadores?${next}`;
  };

  return (
    <div className="mx-auto grid w-full max-w-5xl gap-6 px-4 py-6">
      <PageHeader
        title="Transformadores"
        description="La misma información del mapa, en una lista. Los datos son referenciales y los carga la distribuidora."
        actions={
          <Button asChild variant="outline">
            <Link href="/">Ver en el mapa</Link>
          </Button>
        }
      />

      <form method="get" className="grid gap-4 rounded-lg border bg-card p-4 sm:grid-cols-4 sm:items-end">
        <div className="grid gap-1.5">
          <Label htmlFor="lista-estado">Estado</Label>
          <select id="lista-estado" name="estado" defaultValue={status ?? ""} className={selectClass}>
            <option value="">Todos</option>
            {TRANSFORMER_STATUSES.map((s) => (
              <option key={s} value={s}>
                {TRANSFORMER_STATUS[s].label}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="lista-zona">Zona</Label>
          <select id="lista-zona" name="zona" defaultValue={zoneId ?? ""} className={selectClass}>
            <option value="">Todas</option>
            {zones.map((z) => (
              <option key={z.id} value={z.id}>
                {z.zoneName}
                {z.province ? ` (${z.province})` : ""}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="lista-disp">Capacidad libre</Label>
          <select id="lista-disp" name="disp" defaultValue={minAvailability ?? ""} className={selectClass}>
            <option value="">Cualquiera</option>
            <option value="30">30 % o más</option>
            <option value="70">70 % o más</option>
          </select>
        </div>
        <Button type="submit">Filtrar</Button>
      </form>

      <details className="rounded-lg border bg-card p-4">
        <summary className="cursor-pointer text-sm font-medium">Qué significa cada estado</summary>
        <MapLegend className="pt-3 text-sm" />
      </details>

      {failed ? (
        <ErrorState title="No se pudo cargar la lista" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : rows.length === 0 ? (
        <EmptyState title="No hay transformadores con esos filtros" description="Prueba con otra zona o quita algún filtro." />
      ) : (
        <>
          <p role="status" className="text-sm text-muted-foreground">
            {formatCount(rows.length, "transformador", "transformadores")}
            {result?.truncated ? ` (se muestran los primeros ${MAX_ROWS.toLocaleString("es-DO")}; filtra por zona para ver el resto)` : ""}. Página {current} de{" "}
            {pages}.
          </p>
          <div className="rounded-lg border bg-card">
            <Table label="Transformadores">
              <TableHeader>
                <TableRow>
                  <TableHead scope="col">Código</TableHead>
                  <TableHead scope="col">Estado</TableHead>
                  <TableHead scope="col" className="text-right">
                    Capacidad libre
                  </TableHead>
                  <TableHead scope="col">
                    <span className="sr-only">Acciones</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {visible.map((t) => (
                  <TableRow key={t.id}>
                    <TableCell>
                      <Link href={`/transformadores/${t.id}`} className="font-mono text-link underline underline-offset-4 hover:no-underline">
                        {t.code}
                      </Link>
                    </TableCell>
                    <TableCell>
                      <TransformerStatusBadge status={t.status ?? ""} />
                    </TableCell>
                    <TableCell className="text-right">
                      <AvailabilityValue pct={t.availabilityPct} status={t.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <Link href={`/?t=${t.id}`} className="text-sm text-link underline underline-offset-4 hover:no-underline">
                        Ver en el mapa<span className="sr-only"> ({t.code})</span>
                      </Link>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          {pages > 1 && (
            <nav aria-label="Páginas" className="flex flex-wrap items-center justify-between gap-2">
              {current > 1 ? (
                <Button asChild variant="outline" size="sm">
                  <Link href={pageHref(current - 1)}>Anterior</Link>
                </Button>
              ) : (
                <span />
              )}
              <span className="text-sm text-muted-foreground">
                Página {current} de {pages}
              </span>
              {current < pages ? (
                <Button asChild variant="outline" size="sm">
                  <Link href={pageHref(current + 1)}>Siguiente</Link>
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
