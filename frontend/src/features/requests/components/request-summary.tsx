import Link from "next/link";
import type { ReactNode } from "react";

import { KvaValue } from "@/components/shared/kva-value";
import { REQUEST_TYPE_LABEL } from "@/config/status";
import type { components } from "@/lib/api/schema";
import { formatDate } from "@/lib/format";

import {
  GENERATION_SYSTEM_OPTIONS,
  GENERATOR_KIND_OPTIONS,
  INTERCONNECTION_TYPE_OPTIONS,
  labelOf,
  PROPERTY_RELATION_OPTIONS,
  TARIFF_TYPE_OPTIONS,
  USAGE_TYPE_OPTIONS,
} from "../labels";

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="grid gap-0.5 sm:grid-cols-[12rem_1fr]">
      <dt className="text-muted-foreground">{label}</dt>
      <dd>{children || "Sin dato"}</dd>
    </div>
  );
}

type Props = {
  request: components["schemas"]["ConnectionRequestResponseDto"];
  /** Para la revisión: contacto del dueño y del ingeniero, servicio actual y cada equipo de generación. */
  detailed?: boolean;
};

/** Datos de la solicitud. La usan el expediente del solicitante y la revisión del personal. */
export function RequestSummary({ request, detailed = false }: Props) {
  const net = request.netMetering;
  const join = (...parts: (string | null | undefined | false)[]) => parts.filter(Boolean).join(" · ");

  return (
    <section aria-labelledby="datos" className="grid gap-3">
      <h2 id="datos" className="text-lg font-semibold">
        Datos de la solicitud
      </h2>
      <dl className="grid gap-2 rounded-lg border bg-card p-4 text-sm">
        <Row label="Trámite">{REQUEST_TYPE_LABEL[request.requestType as keyof typeof REQUEST_TYPE_LABEL] ?? request.requestType}</Row>
        <Row label="Titular">{request.holderName}</Row>
        {request.installerCompanyName && <Row label="Instaladora">{request.installerCompanyName}</Row>}
        <Row label="Transformador">
          {request.transformerId ? (
            <Link href={`/transformadores/${request.transformerId}`} className="font-mono text-link underline underline-offset-4 hover:no-underline">
              {request.transformerCode}
            </Link>
          ) : null}
        </Row>
        <Row label="Capacidad">
          <KvaValue value={request.requestedCapacityKva} />
        </Row>
        <Row label="Uso y tarifa">
          {labelOf(USAGE_TYPE_OPTIONS, request.usageType)}, {labelOf(TARIFF_TYPE_OPTIONS, request.tariffType)}
        </Row>
        <Row label="Interconexión">{labelOf(INTERCONNECTION_TYPE_OPTIONS, request.interconnectionType)}</Row>
        <Row label="Dirección">{request.projectAddress}</Row>
        {detailed && (
          <Row label="Ubicación">
            <span className="font-mono text-xs tabular-nums">
              {request.latitude.toFixed(5)}, {request.longitude.toFixed(5)}
            </span>
          </Row>
        )}
        {request.projectDescription && <Row label="Descripción">{request.projectDescription}</Row>}
        <Row label="Relación con el inmueble">{labelOf(PROPERTY_RELATION_OPTIONS, request.propertyRelation)}</Row>
        {request.ownerFullName && (
          <Row label="Dueño">
            {detailed ? join(request.ownerFullName, request.ownerNationalId && `cédula ${request.ownerNationalId}`, request.ownerPhone) : request.ownerFullName}
          </Row>
        )}
        {request.engineerFullName && (
          <Row label="Ingeniero">
            {join(
              `${request.engineerFullName} (CODIA ${request.engineerCodiaNumber})`,
              detailed && request.engineerCompany,
              detailed && request.engineerPhone,
              detailed && request.engineerEmail,
            )}
          </Row>
        )}
        {detailed && (request.serviceContractNumber || request.meterNumber) && (
          <Row label="Servicio actual">
            {join(request.serviceContractNumber && `contrato ${request.serviceContractNumber}`, request.meterNumber && `medidor ${request.meterNumber}`)}
          </Row>
        )}
        {net && (
          <Row label="Generación">
            {join(
              `${labelOf(GENERATION_SYSTEM_OPTIONS, net.generationSystemType)}, ${net.installedCapacityKw} kW, ${net.generators.length} equipo(s)`,
              detailed && net.comments,
            )}
          </Row>
        )}
        <Row label="Creada">{formatDate(request.requestedAt)}</Row>
      </dl>
      {detailed && net && net.generators.length > 0 && (
        <ul className="grid gap-2 text-sm" aria-label="Equipos de generación">
          {net.generators.map((g, i) => (
            <li key={i} className="rounded-md border p-3">
              <p className="font-medium">
                Equipo {i + 1}: {join(g.manufacturer, g.inverterModel, labelOf(GENERATOR_KIND_OPTIONS, g.generatorKind))}
              </p>
              <p className="font-mono text-xs text-muted-foreground tabular-nums">
                {join(
                  `${g.ratedPowerKva} kVA`,
                  `${g.connectionVoltage} V`,
                  `${g.nominalCurrent} A`,
                  g.phases === 3 ? "trifásico" : "monofásico",
                  `FP ${g.powerFactorMin}–${g.powerFactorMax}`,
                  `THD ${g.harmonicDistortionPercent} %`,
                  g.certificationStandard,
                  g.serialNumber && `serie ${g.serialNumber}`,
                )}
              </p>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
