import { TriangleAlert } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { KvaValue } from "@/components/shared/kva-value";
import { RequestStatusBadge } from "@/components/shared/request-status-badge";
import { TransformerStatusBadge } from "@/components/shared/transformer-status-badge";
import { can } from "@/config/permissions";
import { asRequestStatus, REQUEST_STATUS } from "@/config/status";
import { getTransformerById, ReviewActions } from "@/features/management";
import { DocumentsPanel, getRequest, getRequestFile, RequestSummary, RequestTimeline } from "@/features/requests";
import { getSession, requireAction } from "@/server/session";

export async function generateMetadata({ params }: PageProps<"/gestion/solicitudes/[id]">): Promise<Metadata> {
  const session = await getSession();
  const request = session ? await getRequest(session, (await params).id) : null;
  return { title: request ? `Revisión ${request.requestCode}` : "Solicitud no encontrada" };
}

/**
 * Revisión de un expediente por el personal (RF-12, RF-13): datos completos, documentos, capacidad del transformador,
 * historial y las transiciones que el rol puede hacer. Los borradores no existen para el personal (404).
 */
export default async function ReviewRequestPage({ params }: PageProps<"/gestion/solicitudes/[id]">) {
  const { id } = await params;
  const session = await requireAction("gestion", `/gestion/solicitudes/${id}`, "viewOthersRequests");
  const request = await getRequest(session, id);
  if (!request) notFound();

  const [{ required, documents, history }, transformer] = await Promise.all([
    getRequestFile(session, request.id),
    request.transformerId ? getTransformerById(String(request.transformerId)) : Promise.resolve(null),
  ]);
  const status = asRequestStatus(request.status);
  const effective = transformer ? transformer.availableCapacityKva - transformer.reservedCapacityKva : null;
  // En revisión, la capacidad de esta solicitud ya está dentro de lo reservado.
  const short = request.status === "Received" && effective !== null && effective < request.requestedCapacityKva;

  return (
    <div className="grid grid-cols-1 gap-6">
      <nav aria-label="Ruta" className="text-sm">
        <Link href="/gestion/solicitudes" className="text-link underline underline-offset-4 hover:no-underline">
          Solicitudes
        </Link>
      </nav>

      <header className="grid gap-2">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-mono text-2xl font-semibold tracking-tight">{request.requestCode}</h1>
          <RequestStatusBadge status={request.status} />
        </div>
        {status && <p className="max-w-prose text-muted-foreground">{REQUEST_STATUS[status].meaning}</p>}
      </header>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="grid min-w-0 content-start gap-6">
          <RequestSummary request={request} detailed />
          <DocumentsPanel requestId={request.id} required={required} documents={documents} canChange={false} downloadBase="/gestion/solicitudes" />
        </div>

        <div className="grid min-w-0 content-start gap-6">
          <ReviewActions
            requestId={request.id}
            status={request.status}
            canReview={can(session.role, "reviewRequests")}
            canDecide={can(session.role, "decideRequests")}
          />

          {transformer && (
            <section aria-labelledby="capacidad" className="grid gap-3 rounded-lg border bg-card p-4 text-sm">
              <h2 id="capacidad" className="font-semibold">
                Capacidad del transformador
              </h2>
              <div className="flex flex-wrap items-center gap-2">
                <span className="font-mono font-semibold">{transformer.code}</span>
                <TransformerStatusBadge status={transformer.status} />
              </div>
              <dl className="grid gap-1">
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">Disponible informada</dt>
                  <dd>
                    <KvaValue value={transformer.availableCapacityKva} />
                  </dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">Reservada por solicitudes</dt>
                  <dd>
                    <KvaValue value={transformer.reservedCapacityKva} />
                  </dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">Libre para nuevas</dt>
                  <dd>
                    <KvaValue value={effective} />
                  </dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">Pide esta solicitud</dt>
                  <dd>
                    <KvaValue value={request.requestedCapacityKva} />
                  </dd>
                </div>
              </dl>
              {short && (
                <p className="flex items-start gap-2 text-tone-warning-text">
                  <TriangleAlert aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
                  La capacidad libre no cubre lo pedido: al pasar a revisión la API no podrá reservarla (RN-15).
                </p>
              )}
            </section>
          )}

          <RequestTimeline history={history} />
        </div>
      </div>
    </div>
  );
}
