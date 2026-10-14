import { Download } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

import { KvaValue } from "@/components/shared/kva-value";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { asRequestStatus, REQUEST_STATUS, REQUEST_TYPE_LABEL } from "@/config/status";
import { CountChart, formatDays, getInstallerRanking, getStatistics, MonthlyChart, monthlyPoints, periodFrom } from "@/features/reports";
import { formatDate, formatPercent } from "@/lib/format";
import { ApiError } from "@/server/api-client";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Reportes" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

function Kpi({ label, value, hint }: { label: string; value: React.ReactNode; hint?: string }) {
  return (
    <div className="grid gap-1 rounded-lg border bg-card p-4">
      <dt className="text-sm text-muted-foreground">{label}</dt>
      <dd className="font-mono text-xl font-semibold tabular-nums">{value}</dd>
      {hint && <dd className="text-xs text-muted-foreground">{hint}</dd>}
    </div>
  );
}

function CountTable({ caption, rows }: { caption: string; rows: { label: string; count: number }[] }) {
  return (
    <Table label={caption}>
      <TableHeader>
        <TableRow>
          <TableHead scope="col">{caption}</TableHead>
          <TableHead scope="col" className="text-right">
            Solicitudes
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {rows.map((r) => (
          <TableRow key={r.label}>
            <TableCell>{r.label}</TableCell>
            <TableCell className="text-right font-mono tabular-nums">{r.count}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

/** Estadísticas de solicitudes (RF-27), ranking de instaladoras (RF-28) y exportación (RF-30). Período en la URL. */
export default async function ReportsPage({ searchParams }: PageProps<"/gestion/reportes">) {
  const session = await requireAction("gestion", "/gestion/reportes", "viewReports");
  const params = await searchParams;
  const { period, error } = periodFrom(one(params.desde), one(params.hasta));

  let stats: Awaited<ReturnType<typeof getStatistics>> | null = null;
  let ranking: Awaited<ReturnType<typeof getInstallerRanking>> = [];
  let failure: string | null = error;
  if (!error) {
    try {
      [stats, ranking] = await Promise.all([getStatistics(session, period), getInstallerRanking(session, period)]);
    } catch (e) {
      if (!(e instanceof ApiError)) throw e;
      failure = e.errors.join(" ") || e.message;
    }
  }

  const exportHref = (format: "Xlsx" | "Pdf") => `/gestion/reportes/exportar?${new URLSearchParams({ formato: format, desde: period.from, hasta: period.to })}`;
  const byStatus = (stats?.byStatus ?? []).map((i) => {
    const status = asRequestStatus(i.key);
    return { label: status ? REQUEST_STATUS[status].label : i.key, count: i.count };
  });
  const byType = (stats?.byType ?? []).map((i) => ({ label: REQUEST_TYPE_LABEL[i.key as keyof typeof REQUEST_TYPE_LABEL] ?? i.key, count: i.count }));
  const byZone = (stats?.byZone ?? []).map((i) => ({ label: i.key || "Sin zona", count: i.count })).sort((a, b) => b.count - a.count);
  const monthly = monthlyPoints(stats?.byMonth ?? []);

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader
        title="Reportes"
        description={`Solicitudes enviadas del ${formatDate(`${period.from}T12:00:00Z`)} al ${formatDate(`${period.to}T12:00:00Z`)}. Los borradores no cuentan.`}
        actions={
          !failure && (
            <>
              <Button asChild variant="outline">
                <a href={exportHref("Xlsx")} download>
                  <Download aria-hidden="true" /> Excel
                </a>
              </Button>
              <Button asChild variant="outline">
                <a href={exportHref("Pdf")} download>
                  <Download aria-hidden="true" /> PDF
                </a>
              </Button>
            </>
          )
        }
      />

      <form method="get" className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-4">
        <div className="grid gap-1.5">
          <Label htmlFor="r-desde">Desde</Label>
          <Input id="r-desde" name="desde" type="date" defaultValue={period.from} />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="r-hasta">Hasta</Label>
          <Input id="r-hasta" name="hasta" type="date" defaultValue={period.to} />
        </div>
        <Button type="submit">Ver</Button>
        <Button asChild variant="ghost">
          <Link href="/gestion/reportes">Últimos 12 meses</Link>
        </Button>
      </form>

      {failure ? (
        <ErrorState title="No se pudo calcular el reporte" description={failure} />
      ) : !stats || stats.total === 0 ? (
        <EmptyState title="No hay solicitudes en ese período" description="Prueba con otras fechas." />
      ) : (
        <>
          <section aria-labelledby="resumen" className="grid gap-3">
            <h2 id="resumen" className="sr-only">
              Resumen
            </h2>
            <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <Kpi label="Solicitudes" value={stats.total} hint={`${stats.pending} en curso · ${stats.cancelled} canceladas`} />
              <Kpi
                label="Tasa de aprobación"
                value={stats.approvalRatePct == null ? "Sin dato" : formatPercent(stats.approvalRatePct)}
                hint={`${stats.approved} aprobadas · ${stats.rejected} rechazadas`}
              />
              <Kpi label="Capacidad aprobada" value={<KvaValue value={stats.approvedKva} />} hint={`de ${stats.requestedKva.toLocaleString("es-DO")} kVA pedidos`} />
              <Kpi
                label="Tiempo de resolución"
                value={formatDays(stats.avgDaysToResolution)}
                hint={`Mediana ${formatDays(stats.medianDaysToResolution)} · primera revisión ${formatDays(stats.avgDaysToFirstReview)}`}
              />
            </dl>
          </section>

          <section aria-labelledby="por-mes" className="grid grid-cols-1 gap-3 rounded-lg border bg-card p-4">
            <h2 id="por-mes" className="font-semibold">
              Solicitudes por mes
            </h2>
            <MonthlyChart data={monthly} description="Gráfico de barras con las solicitudes de cada mes: aprobadas, rechazadas y el resto. La tabla siguiente tiene los mismos datos." />
            <details className="text-sm">
              <summary className="cursor-pointer text-muted-foreground">Ver los datos en una tabla</summary>
              <Table label="Solicitudes por mes">
                <TableHeader>
                  <TableRow>
                    <TableHead scope="col">Mes</TableHead>
                    <TableHead scope="col" className="text-right">
                      Total
                    </TableHead>
                    <TableHead scope="col" className="text-right">
                      Aprobadas
                    </TableHead>
                    <TableHead scope="col" className="text-right">
                      Rechazadas
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {monthly.map((m) => (
                    <TableRow key={m.month}>
                      <TableCell>{m.label}</TableCell>
                      <TableCell className="text-right font-mono tabular-nums">{m.approved + m.rejected + m.others}</TableCell>
                      <TableCell className="text-right font-mono tabular-nums">{m.approved}</TableCell>
                      <TableCell className="text-right font-mono tabular-nums">{m.rejected}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </details>
          </section>

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <section aria-labelledby="por-estado" className="grid min-w-0 grid-cols-1 content-start gap-3 rounded-lg border bg-card p-4">
              <h2 id="por-estado" className="font-semibold">
                Por estado
              </h2>
              <CountChart data={byStatus} description="Gráfico de barras con las solicitudes por estado. La tabla siguiente tiene los mismos datos." />
              <CountTable caption="Estado" rows={byStatus} />
            </section>
            <section aria-labelledby="por-tipo-zona" className="grid min-w-0 grid-cols-1 content-start gap-3 rounded-lg border bg-card p-4">
              <h2 id="por-tipo-zona" className="font-semibold">
                Por trámite y por zona
              </h2>
              <CountTable caption="Trámite" rows={byType} />
              <CountTable caption="Zona" rows={byZone} />
            </section>
          </div>

          <section aria-labelledby="ranking" className="grid grid-cols-1 gap-3">
            <h2 id="ranking" className="text-lg font-semibold">
              Instaladoras más activas
            </h2>
            {ranking.length === 0 ? (
              <p className="text-sm text-muted-foreground">Ninguna instaladora presentó solicitudes en el período.</p>
            ) : (
              <div className="rounded-lg border bg-card">
                <Table label="Ranking de instaladoras">
                  <TableHeader>
                    <TableRow>
                      <TableHead scope="col">#</TableHead>
                      <TableHead scope="col">Empresa</TableHead>
                      <TableHead scope="col" className="text-right">
                        Solicitudes
                      </TableHead>
                      <TableHead scope="col" className="text-right">
                        Aprobadas
                      </TableHead>
                      <TableHead scope="col" className="text-right">
                        Aprobación
                      </TableHead>
                      <TableHead scope="col" className="text-right">
                        Capacidad pedida
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {ranking.map((r) => (
                      <TableRow key={r.installerCompanyId}>
                        <TableCell className="font-mono tabular-nums">{r.rank}</TableCell>
                        <TableCell className="font-medium">{r.tradeName}</TableCell>
                        <TableCell className="text-right font-mono tabular-nums">{r.total}</TableCell>
                        <TableCell className="text-right font-mono tabular-nums">{r.approved}</TableCell>
                        <TableCell className="text-right font-mono tabular-nums">{r.approvalRatePct == null ? "Sin dato" : formatPercent(r.approvalRatePct)}</TableCell>
                        <TableCell className="text-right">
                          <KvaValue value={r.requestedKva} />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
