import { Download, Info } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import type { ReactNode } from "react";

import { KvaValue } from "@/components/shared/kva-value";
import { TransformerStatusBadge } from "@/components/shared/transformer-status-badge";
import { Button } from "@/components/ui/button";
import {
  formatDistance,
  formatKw,
  formatKwh,
  formatPayback,
  formatRoi,
  getSimulation,
  viabilityOf,
  ViabilityBadge,
} from "@/features/simulations";
import { formatDateTime, formatMoney, formatPercent } from "@/lib/format";
import { getSession, requireZone } from "@/server/session";

export async function generateMetadata({ params }: PageProps<"/portal/simulaciones/[id]">): Promise<Metadata> {
  const session = await getSession();
  const simulation = session ? await getSimulation(session, (await params).id) : null;
  return { title: simulation ? simulation.name || `Simulación ${simulation.id}` : "Simulación no encontrada" };
}

function Kpi({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="grid gap-1 rounded-lg border bg-card p-4">
      <dt className="text-sm text-muted-foreground">{label}</dt>
      <dd className="font-mono text-xl font-semibold tabular-nums">{value}</dd>
      {hint && <dd className="text-xs text-muted-foreground">{hint}</dd>}
    </div>
  );
}

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-3 py-1.5">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="text-right font-mono tabular-nums">{children}</dd>
    </div>
  );
}

/** Lee un supuesto numérico guardado con la simulación; la forma del objeto depende de la versión de la fórmula. */
function assumption(assumptions: unknown, key: string): number | null {
  const value = assumptions && typeof assumptions === "object" ? (assumptions as Record<string, unknown>)[key] : undefined;
  return typeof value === "number" ? value : null;
}

/**
 * Resultado de una simulación (RF-19 a RF-25). Todo viene calculado por la API con la versión de fórmula guardada;
 * aquí solo se da formato. Aviso informativo siempre visible.
 */
export default async function SimulationPage({ params }: PageProps<"/portal/simulaciones/[id]">) {
  const { id } = await params;
  const session = await requireZone("portal", `/portal/simulaciones/${id}`);
  const simulation = await getSimulation(session, id);
  if (!simulation) notFound();

  const viability = viabilityOf(simulation.viabilityStatus);
  const reference = simulation.referenceTransformer;
  // RN-03: saturado o en mantenimiento no admite solicitudes. La API vuelve a comprobarlo al crear el borrador.
  const acceptsRequests = reference != null && reference.status !== "Saturated" && reference.status !== "Maintenance";
  const yieldKwp = assumption(simulation.assumptions, "specificYieldKwhPerKwp");
  const costPerKwp = assumption(simulation.assumptions, "costPerKwp");
  const lifetime = assumption(simulation.assumptions, "lifetimeYears");
  const surplusPct = assumption(simulation.assumptions, "surplusPaymentPct");

  return (
    <div className="grid grid-cols-1 gap-6">
      <nav aria-label="Ruta" className="text-sm">
        <Link href="/portal/simulaciones" className="text-link underline underline-offset-4 hover:no-underline">
          Simulaciones
        </Link>
      </nav>

      <header className="grid gap-2">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-semibold tracking-tight">{simulation.name || `Simulación ${simulation.id}`}</h1>
          <ViabilityBadge status={simulation.viabilityStatus} />
        </div>
        <p className="max-w-prose text-muted-foreground">{viability.meaning}</p>
        <p className="text-sm text-muted-foreground">
          Calculada el {formatDateTime(simulation.createdAt)} con la fórmula {simulation.formulaVersion}.
        </p>
      </header>

      <p role="note" className="flex items-start gap-2 rounded-lg bg-tone-info-soft p-4 text-sm text-tone-info-text">
        <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
        {simulation.disclaimer}
      </p>

      <section aria-labelledby="resumen" className="grid gap-3">
        <h2 id="resumen" className="sr-only">
          Resumen
        </h2>
        <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Kpi label="Ahorro anual" value={formatMoney(simulation.annualSavings)} hint="Incluye el pago del excedente" />
          <Kpi label="Retorno de la inversión" value={formatPayback(simulation.paybackYears)} />
          <Kpi label="ROI" value={formatRoi(simulation.roi)} hint={lifetime ? `En ${lifetime} años de vida útil` : undefined} />
          <Kpi label="Cobertura del consumo" value={formatPercent(simulation.coveragePct)} />
        </dl>
      </section>

      <div className="grid gap-6 lg:grid-cols-2">
        <section aria-labelledby="detalle" className="grid content-start gap-3">
          <h2 id="detalle" className="text-lg font-semibold">
            Detalle del cálculo
          </h2>
          <dl className="grid divide-y rounded-lg border bg-card px-4 py-2 text-sm">
            <Row label="Potencia instalada">{formatKw(simulation.installedCapacityKw)}</Row>
            <Row label="Producción anual">{formatKwh(simulation.annualProductionKwh)}</Row>
            <Row label="Consumo anual">{formatKwh(simulation.annualConsumptionKwh)}</Row>
            <Row label="Factura de energía sin paneles">{formatMoney(simulation.annualBillBefore)}</Row>
            <Row label="Factura de energía con paneles">{formatMoney(simulation.annualBillAfter)}</Row>
            <Row label="Excedente a fin de año">{formatKwh(simulation.annualExportCreditKwh)}</Row>
            <Row label="Pago del excedente">{formatMoney(simulation.surplusPayment)}</Row>
            <Row label="Inversión">{formatMoney(simulation.investmentCost)}</Row>
          </dl>
        </section>

        <section aria-labelledby="recomendaciones" className="grid content-start gap-3">
          <h2 id="recomendaciones" className="text-lg font-semibold">
            Recomendaciones
          </h2>
          {simulation.recommendations.length === 0 ? (
            <p className="text-sm text-muted-foreground">Sin recomendaciones: los datos están dentro de lo esperado.</p>
          ) : (
            <ul className="grid list-disc gap-2 pl-5 text-sm">
              {simulation.recommendations.map((r) => (
                <li key={r}>{r}</li>
              ))}
            </ul>
          )}
        </section>
      </div>

      <section aria-labelledby="transformador" className="grid gap-3">
        <h2 id="transformador" className="text-lg font-semibold">
          Transformador de referencia
        </h2>
        {reference ? (
          <div className="grid gap-3 rounded-lg border bg-card p-4">
            <div className="flex flex-wrap items-center gap-3">
              <Link href={`/transformadores/${reference.id}`} className="font-mono font-semibold text-link underline underline-offset-4 hover:no-underline">
                {reference.code}
              </Link>
              <TransformerStatusBadge status={reference.status} />
              <span className="text-sm text-muted-foreground">a {formatDistance(reference.distanceMeters)}</span>
              <span className="text-sm text-muted-foreground">
                {/* En mantenimiento la disponibilidad no se calcula (RN-02), igual que en el mapa. */}
                Disponible: {reference.status === "Maintenance" ? "no se calcula" : <KvaValue value={reference.effectiveAvailableKva} />}
              </span>
            </div>
            <p className="text-sm text-muted-foreground">
              El más cercano al calcular. Es solo una referencia: la solicitud se evalúa con los datos del momento.
            </p>
            {!acceptsRequests && (
              <p className="text-sm text-tone-warning-text">
                Este transformador no acepta solicitudes nuevas ahora. Para solicitar, elige en el mapa otro cercano que sí las acepte.
              </p>
            )}
            <div className="flex flex-wrap gap-2">
              {acceptsRequests && (
                <Button asChild>
                  <Link href={`/portal/solicitudes/nueva?transformador=${reference.id}&simulacion=${simulation.id}`}>
                    Solicitar medición neta con estos datos
                  </Link>
                </Button>
              )}
              <Button asChild variant="outline">
                <Link href={`/?t=${reference.id}`}>{acceptsRequests ? "Ver en el mapa" : "Elegir otro en el mapa"}</Link>
              </Button>
            </div>
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">No hay transformadores cerca de esa ubicación. Revisa las coordenadas en el mapa.</p>
        )}
      </section>

      {simulation.equipment.length > 0 && (
        <section aria-labelledby="equipos" className="grid gap-3">
          <h2 id="equipos" className="text-lg font-semibold">
            Equipos
          </h2>
          <ul className="grid divide-y rounded-lg border bg-card text-sm">
            {simulation.equipment.map((e, i) => (
              <li key={i} className="flex flex-wrap justify-between gap-2 p-3">
                <span>{e.inverterModel && !e.panelModel ? `Inversor ${e.inverterModel}` : `Paneles${e.panelModel ? ` ${e.panelModel}` : ""}`}</span>
                <span className="font-mono tabular-nums">
                  {e.quantity} × {e.ratedPowerKw} kW
                </span>
              </li>
            ))}
          </ul>
        </section>
      )}

      <details className="rounded-lg border p-4 text-sm">
        <summary className="cursor-pointer font-medium">Supuestos del cálculo</summary>
        <dl className="mt-3 grid gap-1">
          {yieldKwp != null && <Row label="Producción por kWp al año">{formatKwh(yieldKwp)}</Row>}
          {costPerKwp != null && <Row label="Costo estimado por kWp">{formatMoney(costPerKwp)}</Row>}
          {surplusPct != null && <Row label="Pago del excedente">{formatPercent(surplusPct)} del primer tramo</Row>}
          {lifetime != null && <Row label="Vida útil">{lifetime} años</Row>}
          {simulation.flatPricePerKwh != null && <Row label="Precio fijo por kWh">{formatMoney(simulation.flatPricePerKwh)}</Row>}
          <Row label="Consumo mensual">{formatKwh(simulation.monthlyConsumptionKwh)}</Row>
        </dl>
      </details>

      <div className="flex flex-wrap gap-2">
        <Button asChild variant="outline">
          <a href={`/portal/simulaciones/${simulation.id}/reporte`} download>
            <Download aria-hidden="true" /> Descargar en PDF
          </a>
        </Button>
        <Button asChild variant="ghost">
          <Link href="/portal/simulaciones/nueva">Hacer otra simulación</Link>
        </Button>
      </div>
    </div>
  );
}
