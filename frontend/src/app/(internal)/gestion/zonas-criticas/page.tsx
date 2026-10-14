import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { getCriticalZones } from "@/features/reports";
import { formatPercent } from "@/lib/format";
import { ApiError } from "@/server/api-client";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Zonas críticas" };

/**
 * Zonas con transformadores críticos o saturados (RF-29), de la más comprometida a la menos. Las zonas no tienen
 * polígono en la base: el enlace al mapa muestra sus transformadores filtrados, no el contorno de la zona.
 */
export default async function CriticalZonesPage() {
  const session = await requireAction("gestion", "/gestion/zonas-criticas", "viewReports");

  let zones: Awaited<ReturnType<typeof getCriticalZones>> | null = null;
  try {
    zones = await getCriticalZones(session);
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  }

  const rows = (zones ?? [])
    .map((z) => ({ ...z, atRisk: z.critical + z.saturated, share: z.totalTransformers > 0 ? ((z.critical + z.saturated) / z.totalTransformers) * 100 : 0 }))
    .sort((a, b) => b.share - a.share || b.atRisk - a.atRisk);

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader
        title="Zonas críticas"
        description="Zonas con transformadores en estado crítico (menos de 30 % libre) o saturado (sin capacidad), ordenadas por la proporción en riesgo."
      />

      {!zones ? (
        <ErrorState title="No se pudieron cargar las zonas" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : rows.length === 0 ? (
        <EmptyState title="Ninguna zona tiene transformadores críticos o saturados" />
      ) : (
        <div className="rounded-lg border bg-card">
          <Table label="Zonas críticas">
            <TableHeader>
              <TableRow>
                <TableHead scope="col">Zona</TableHead>
                <TableHead scope="col" className="text-right">
                  Transformadores
                </TableHead>
                <TableHead scope="col" className="text-right">
                  Críticos
                </TableHead>
                <TableHead scope="col" className="text-right">
                  Saturados
                </TableHead>
                <TableHead scope="col" className="text-right">
                  En riesgo
                </TableHead>
                <TableHead scope="col">
                  <span className="sr-only">Ver</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((z) => (
                <TableRow key={z.serviceZoneId}>
                  <TableCell className="font-medium">{z.zoneName}</TableCell>
                  <TableCell className="text-right font-mono tabular-nums">{z.totalTransformers.toLocaleString("es-DO")}</TableCell>
                  <TableCell className="text-right font-mono tabular-nums">{z.critical}</TableCell>
                  <TableCell className="text-right font-mono tabular-nums">{z.saturated}</TableCell>
                  <TableCell className="text-right font-mono tabular-nums">{formatPercent(z.share)}</TableCell>
                  <TableCell className="space-x-3 text-right text-sm whitespace-nowrap">
                    <Link
                      href={`/gestion/transformadores?zona=${z.serviceZoneId}&orden=Availability`}
                      className="text-link underline underline-offset-4 hover:no-underline"
                    >
                      Lista<span className="sr-only"> de {z.zoneName}</span>
                    </Link>
                    <Link href={`/?zona=${z.serviceZoneId}`} className="text-link underline underline-offset-4 hover:no-underline">
                      Mapa<span className="sr-only"> de {z.zoneName}</span>
                    </Link>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
