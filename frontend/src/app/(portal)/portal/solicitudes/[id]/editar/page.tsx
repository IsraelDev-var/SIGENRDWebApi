import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";

import { PageHeader } from "@/components/shared/page-header";
import { ErrorState } from "@/components/shared/page-states";
import { formFromRequest, getRequest, getTransformer, RequestWizard } from "@/features/requests";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Editar borrador" };

/** Editar los datos mientras la solicitud es un borrador (D-4). Después de enviarla, los datos ya no cambian. */
export default async function EditDraftPage({ params }: PageProps<"/portal/solicitudes/[id]/editar">) {
  const { id } = await params;
  const session = await requireZone("portal", `/portal/solicitudes/${id}/editar`);
  const request = await getRequest(session, id);
  if (!request) notFound();
  if (request.status !== "Draft") redirect(`/portal/solicitudes/${request.id}`);

  const transformer = request.transformerId ? await getTransformer(String(request.transformerId)) : null;

  return (
    <div className="grid max-w-3xl gap-6">
      <nav aria-label="Ruta" className="text-sm">
        <Link href={`/portal/solicitudes/${request.id}`} className="text-link underline underline-offset-4 hover:no-underline">
          Volver al expediente {request.requestCode}
        </Link>
      </nav>
      <PageHeader title={`Editar ${request.requestCode}`} description="Los documentos subidos se mantienen. Revísalos si cambias el trámite o la capacidad." />
      {transformer ? (
        <RequestWizard
          requestId={request.id}
          transformer={{
            id: transformer.id,
            code: transformer.code,
            distributorId: transformer.distributorId,
            status: transformer.status,
            availableCapacityKva: transformer.availableCapacityKva,
          }}
          initialValues={formFromRequest(request)}
          needsHolder={false}
          holderName={request.holderName}
        />
      ) : (
        <ErrorState title="No encontramos el transformador de esta solicitud" description="Descarta el borrador y crea uno nuevo desde el mapa." />
      )}
    </div>
  );
}
