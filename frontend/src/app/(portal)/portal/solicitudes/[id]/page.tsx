import { CircleAlert, CircleCheck } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { RequestStatusBadge } from "@/components/shared/request-status-badge";
import { asRequestStatus, REQUEST_STATUS } from "@/config/status";
import { DocumentsPanel, getRequest, getRequestFile, RequestActions, RequestSummary, RequestTimeline } from "@/features/requests";
import { getSession, requireZone } from "@/server/session";

export async function generateMetadata({ params }: PageProps<"/portal/solicitudes/[id]">): Promise<Metadata> {
  const session = await getSession();
  const request = session ? await getRequest(session, (await params).id) : null;
  return { title: request ? `Solicitud ${request.requestCode}` : "Solicitud no encontrada" };
}

// RequestWorkflow.AllowsDocumentChanges: después de enviar a revisión o de aprobada, el expediente no cambia (RN-19).
const DOCUMENTS_OPEN = ["Draft", "Received", "InCorrection"];

/** Expediente de una solicitud propia (RF-13, RN-22): datos, documentos, acciones según el estado e historial. */
export default async function RequestFilePage({ params, searchParams }: PageProps<"/portal/solicitudes/[id]">) {
  const { id } = await params;
  const session = await requireZone("portal", `/portal/solicitudes/${id}`);
  const request = await getRequest(session, id);
  if (!request) notFound();

  const [{ required, documents, history }, query] = await Promise.all([getRequestFile(session, request.id), searchParams]);
  const status = asRequestStatus(request.status);
  const lastComment = [...history].reverse().find((h) => h.newState === request.status)?.comment;

  return (
    <div className="grid grid-cols-1 gap-6">
      <nav aria-label="Ruta" className="text-sm">
        <Link href="/portal/solicitudes" className="text-link underline underline-offset-4 hover:no-underline">
          Mis solicitudes
        </Link>
      </nav>

      <header className="grid gap-2">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-mono text-2xl font-semibold tracking-tight">{request.requestCode}</h1>
          <RequestStatusBadge status={request.status} />
        </div>
        {status && <p className="max-w-prose text-muted-foreground">{REQUEST_STATUS[status].meaning}</p>}
      </header>

      {query.guardado === "1" && request.status === "Draft" && (
        <p role="status" className="flex items-start gap-2 rounded-md bg-tone-success-soft px-3 py-2 text-sm text-tone-success-text">
          <CircleCheck aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
          Borrador guardado. Sube los documentos requeridos y envíalo a Edesur.
        </p>
      )}

      {(request.status === "Observed" || request.status === "InCorrection" || request.status === "Rejected") && lastComment && (
        <section
          aria-labelledby="comentario-edesur"
          className="flex items-start gap-3 rounded-lg bg-tone-warning-soft p-4 text-tone-warning-text"
        >
          <CircleAlert aria-hidden="true" className="mt-0.5 size-5 shrink-0" />
          <div className="grid gap-1">
            <h2 id="comentario-edesur" className="font-medium">
              {request.status === "Rejected" ? "Motivo del rechazo" : "Lo que pidió Edesur"}
            </h2>
            <p className="text-sm">{lastComment}</p>
          </div>
        </section>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <div className="grid min-w-0 content-start gap-6">
          <RequestSummary request={request} />

          <DocumentsPanel requestId={request.id} required={required} documents={documents} canChange={DOCUMENTS_OPEN.includes(request.status)} />
        </div>

        <div className="grid min-w-0 content-start gap-6">
          <RequestActions requestId={request.id} status={request.status} documentsComplete={required.complete} />
          <RequestTimeline history={history} />
        </div>
      </div>
    </div>
  );
}
