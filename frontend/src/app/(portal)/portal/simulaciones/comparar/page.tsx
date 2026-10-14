import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { PageHeader } from "@/components/shared/page-header";
import { ErrorState } from "@/components/shared/page-states";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TONE_BADGE_CLASS } from "@/config/status";
import {
  compareIds,
  compareSimulations,
  formatKw,
  formatKwh,
  formatPayback,
  formatRoi,
  MAX_COMPARE,
  ViabilityBadge,
  type Simulation,
} from "@/features/simulations";
import { formatMoney, formatPercent } from "@/lib/format";
import { ApiError } from "@/server/api-client";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Comparar simulaciones" };

function Best({ children }: { children: ReactNode }) {
  return <Badge className={`${TONE_BADGE_CLASS.success} ml-2`}>{children}</Badge>;
}

/** Comparación de 2 a 5 simulaciones propias (RF-22). La API dice cuál tiene mejor retorno, ROI y ahorro. */
export default async function CompareSimulationsPage({ searchParams }: PageProps<"/portal/simulaciones/comparar">) {
  const session = await requireZone("portal", "/portal/simulaciones/comparar");
  const ids = compareIds((await searchParams).ids);

  const back = (
    <Button asChild variant="outline" className="justify-self-start">
      <Link href="/portal/simulaciones">Volver al historial</Link>
    </Button>
  );

  if (ids.length < 2 || ids.length > MAX_COMPARE) {
    return (
      <div className="grid max-w-2xl gap-6">
        <PageHeader title="Comparar simulaciones" />
        <ErrorState title={`Elige de 2 a ${MAX_COMPARE} simulaciones`} description="Márcalas en el historial y pulsa «Comparar las marcadas»." action={back} />
      </div>
    );
  }

  let comparison: Awaited<ReturnType<typeof compareSimulations>>;
  try {
    comparison = await compareSimulations(session, ids);
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    return (
      <div className="grid max-w-2xl gap-6">
        <PageHeader title="Comparar simulaciones" />
        <ErrorState
          title="No se pudieron comparar"
          description={error.status === 404 ? "Alguna de las simulaciones no existe o no es tuya." : error.message}
          action={back}
        />
      </div>
    );
  }

  const sims = comparison.simulations;
  const rows: { label: string; value: (s: Simulation) => ReactNode }[] = [
    { label: "Resultado", value: (s) => <ViabilityBadge status={s.viabilityStatus} /> },
    {
      label: "Ahorro anual",
      value: (s) => (
        <>
          {formatMoney(s.annualSavings)}
          {s.id === comparison.highestSavingsId && <Best>Mayor ahorro</Best>}
        </>
      ),
    },
    {
      label: "Retorno",
      value: (s) => (
        <>
          {formatPayback(s.paybackYears)}
          {s.id === comparison.bestPaybackId && <Best>Retorno más corto</Best>}
        </>
      ),
    },
    {
      label: "ROI",
      value: (s) => (
        <>
          {formatRoi(s.roi)}
          {s.id === comparison.bestRoiId && <Best>Mejor ROI</Best>}
        </>
      ),
    },
    { label: "Potencia", value: (s) => formatKw(s.installedCapacityKw) },
    { label: "Inversión", value: (s) => formatMoney(s.investmentCost) },
    { label: "Consumo mensual", value: (s) => formatKwh(s.monthlyConsumptionKwh) },
    { label: "Cobertura", value: (s) => formatPercent(s.coveragePct) },
    { label: "Producción anual", value: (s) => formatKwh(s.annualProductionKwh) },
  ];

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader title="Comparar simulaciones" description="Los mismos indicadores lado a lado. Cada una conserva los supuestos con que se calculó." actions={back} />
      <div className="rounded-lg border bg-card">
        <Table label="Comparación de simulaciones">
          <TableHeader>
            <TableRow>
              <TableHead scope="col">Indicador</TableHead>
              {sims.map((s) => (
                <TableHead key={s.id} scope="col">
                  <Link href={`/portal/simulaciones/${s.id}`} className="text-link underline underline-offset-4 hover:no-underline">
                    {s.name || `Simulación ${s.id}`}
                  </Link>
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={row.label}>
                <TableHead scope="row" className="font-normal text-muted-foreground">
                  {row.label}
                </TableHead>
                {sims.map((s) => (
                  <TableCell key={s.id} className="font-mono tabular-nums">
                    {row.value(s)}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
