import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { RequestStatusBadge } from "@/components/shared/request-status-badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { asRequestStatus, REQUEST_STATUS, REQUEST_TYPE_LABEL } from "@/config/status";
import type { components } from "@/lib/api/schema";
import { formatDate } from "@/lib/format";
import { ApiError, apiData } from "@/server/api-client";

export const metadata: Metadata = {
  title: "Seguimiento de solicitud",
  description: "Consulta el estado de una solicitud de conexión o de medición neta con su número de expediente.",
};

type Tracking = components["schemas"]["RequestTrackingDto"];

const MAX_LENGTH = 50;

type Lookup = { kind: "found"; tracking: Tracking } | { kind: "not-found" } | { kind: "error"; message: string; status: number };

async function lookup(code: string): Promise<Lookup> {
  try {
    return { kind: "found", tracking: await apiData<Tracking>(`/ConnectionRequest/track/${encodeURIComponent(code)}`) };
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    if (error.status === 404) return { kind: "not-found" };
    return { kind: "error", message: error.message, status: error.status };
  }
}

/**
 * Seguimiento público por número de expediente (RF-14, §3.3). Solo muestra el estado y las fechas: los datos del
 * solicitante y los documentos quedan en su portal (RN-22). Es un formulario GET: funciona sin JavaScript y el enlace
 * con el expediente se puede compartir.
 */
export default async function TrackingPage({ searchParams }: PageProps<"/seguimiento">) {
  const raw = (await searchParams).expediente;
  const code = (Array.isArray(raw) ? raw[0] : raw)?.trim().toUpperCase() ?? "";
  const invalid = code.length > MAX_LENGTH;
  const result = code && !invalid ? await lookup(code) : null;

  return (
    <div className="mx-auto grid w-full max-w-2xl gap-6 px-4 py-6">
      <PageHeader
        title="Seguimiento de solicitud"
        description="Escribe el número de expediente que recibiste por correo al enviar la solicitud, por ejemplo REQ-1A2B3C4D."
      />

      <form method="get" role="search" className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-4">
        <div className="grid min-w-0 flex-1 gap-1.5">
          <Label htmlFor="seguimiento-expediente">Número de expediente</Label>
          <Input
            id="seguimiento-expediente"
            name="expediente"
            defaultValue={code}
            required
            maxLength={MAX_LENGTH}
            autoComplete="off"
            className="font-mono uppercase"
            aria-invalid={invalid || undefined}
            aria-describedby={invalid ? "seguimiento-error" : undefined}
          />
          {invalid && (
            <p id="seguimiento-error" className="text-sm text-destructive">
              El número de expediente no puede pasar de {MAX_LENGTH} caracteres.
            </p>
          )}
        </div>
        <Button type="submit">Consultar</Button>
      </form>

      <div aria-live="polite">
        {result?.kind === "found" && <TrackingResult tracking={result.tracking} />}
        {result?.kind === "not-found" && (
          <EmptyState
            title={`No encontramos el expediente ${code}`}
            description="Revisa que esté escrito igual que en el correo de confirmación (letras, números y guiones)."
          />
        )}
        {result?.kind === "error" && (
          <ErrorState
            title={result.status === 429 ? "Demasiadas consultas seguidas" : "No se pudo consultar el expediente"}
            description={result.message}
          />
        )}
      </div>

      <p className="text-sm text-muted-foreground">
        ¿Eres el solicitante?{" "}
        <Link href="/ingresar?next=%2Fportal" className="text-link underline underline-offset-4 hover:no-underline">
          Ingresa a tu portal
        </Link>{" "}
        para ver los documentos, las observaciones y el historial completo.
      </p>
    </div>
  );
}

function TrackingResult({ tracking }: { tracking: Tracking }) {
  const status = asRequestStatus(tracking.status);
  const type = tracking.requestType === "NetMetering" || tracking.requestType === "Connection" ? REQUEST_TYPE_LABEL[tracking.requestType] : tracking.requestType;

  return (
    <section aria-labelledby="resultado-expediente" className="grid gap-4 rounded-lg border bg-card p-5">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <div className="grid gap-0.5">
          <p className="text-xs text-muted-foreground">{type}</p>
          <h2 id="resultado-expediente" className="font-mono text-lg font-semibold">
            {tracking.requestCode}
          </h2>
        </div>
        <RequestStatusBadge status={tracking.status} />
      </header>
      {status && <p>{REQUEST_STATUS[status].meaning}</p>}
      <dl className="grid gap-2 text-sm sm:grid-cols-2">
        <div>
          <dt className="text-muted-foreground">Enviada</dt>
          <dd>{formatDate(tracking.requestedAt)}</dd>
        </div>
        <div>
          <dt className="text-muted-foreground">Último cambio</dt>
          <dd>{tracking.lastUpdatedAt ? formatDate(tracking.lastUpdatedAt) : "Sin cambios desde el envío"}</dd>
        </div>
      </dl>
    </section>
  );
}
