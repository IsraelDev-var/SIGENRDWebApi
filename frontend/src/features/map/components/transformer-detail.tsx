import { CircleCheck, CircleX } from "lucide-react";
import Link from "next/link";
import type { ReactNode } from "react";

import { CapacityMeter } from "@/components/shared/capacity-meter";
import { KvaValue } from "@/components/shared/kva-value";
import { LastUpdated } from "@/components/shared/last-updated";
import { TransformerStatusBadge } from "@/components/shared/transformer-status-badge";
import { Button } from "@/components/ui/button";

import type { TransformerDetail as Detail } from "../lib/map-api";

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-3 py-1">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="text-right">{children}</dd>
    </div>
  );
}

/**
 * Detalle técnico del transformador (RF-02, RF-05): todo viene calculado por la API, el frontend no recalcula
 * ni el porcentaje ni el estado. Lo usan el panel del mapa y la ficha /transformadores/[id].
 */
export function TransformerDetail({
  transformer,
  headingLevel = 2,
  applicantActions = true,
}: {
  transformer: Detail;
  headingLevel?: 1 | 2;
  /** Solicitar y simular son del solicitante; en la gestión no se muestran. */
  applicantActions?: boolean;
}) {
  const Heading = headingLevel === 1 ? "h1" : "h2";

  return (
    <article aria-labelledby={`transformador-${transformer.id}`} className="grid gap-4">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <div className="grid gap-0.5">
          <p className="text-xs text-muted-foreground">Transformador</p>
          <Heading id={`transformador-${transformer.id}`} className="font-mono text-lg font-semibold">
            {transformer.code}
          </Heading>
        </div>
        <TransformerStatusBadge status={transformer.status ?? ""} />
      </header>

      <CapacityMeter availabilityPct={transformer.availabilityPct} status={transformer.status ?? ""} />

      <p className="flex items-start gap-2 text-sm">
        {transformer.acceptsNewRequests ? (
          <>
            <CircleCheck aria-hidden="true" className="mt-0.5 size-4 shrink-0 text-status-available" />
            {transformer.status === "Critical"
              ? "Acepta solicitudes nuevas solo si la capacidad disponible cubre la requerida."
              : "Acepta solicitudes nuevas."}
          </>
        ) : (
          <>
            <CircleX aria-hidden="true" className="mt-0.5 size-4 shrink-0 text-status-critical" />
            {transformer.status === "Maintenance"
              ? "En mantenimiento: no acepta solicitudes por ahora."
              : "Sin capacidad libre: no acepta solicitudes nuevas."}
          </>
        )}
      </p>

      {/* Sin sesión, el portal lleva a ingresar y vuelve aquí (proxy.ts). */}
      {applicantActions && (
      <div className="flex flex-wrap gap-2">
        {transformer.acceptsNewRequests && (
          <Button asChild>
            <Link href={`/portal/solicitudes/nueva?transformador=${transformer.id}`}>
              Solicitar conexión<span className="sr-only"> en {transformer.code}</span>
            </Link>
          </Button>
        )}
        {/* La simulación es informativa: se ofrece aunque el transformador no acepte solicitudes. */}
        <Button asChild variant="outline">
          <Link href={`/portal/simulaciones/nueva?lat=${transformer.latitude.toFixed(6)}&lng=${transformer.longitude.toFixed(6)}`}>
            Simular paneles solares<span className="sr-only"> cerca de {transformer.code}</span>
          </Link>
        </Button>
      </div>
      )}

      <dl className="grid divide-y text-sm">
        <Row label="Capacidad total">
          <KvaValue value={transformer.totalCapacityKva} />
        </Row>
        <Row label="Disponible informada">
          <KvaValue value={transformer.availableCapacityKva} />
        </Row>
        <Row label="Reservada por solicitudes">
          <KvaValue value={transformer.reservedCapacityKva} />
        </Row>
        <Row label="Zona">{transformer.zoneName ?? "Sin zona"}</Row>
        <Row label="Distribuidora">{transformer.distributorName ?? "Sin dato"}</Row>
        <Row label="Ubicación">
          <span className="font-mono text-xs tabular-nums">
            {transformer.latitude.toFixed(5)}, {transformer.longitude.toFixed(5)}
          </span>
        </Row>
      </dl>

      <LastUpdated date={transformer.lastUpdated} />
      <p className="text-xs text-muted-foreground">
        Datos referenciales cargados por la distribuidora. La capacidad para tu proyecto se confirma al evaluar la solicitud.
      </p>
    </article>
  );
}
