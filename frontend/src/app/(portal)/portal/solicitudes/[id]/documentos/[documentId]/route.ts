import { downloadFromApi } from "@/server/download";

/** Descarga de un documento del expediente: el token queda en el servidor (BFF) y la API aplica RN-22. */
export async function GET(_request: Request, { params }: RouteContext<"/portal/solicitudes/[id]/documentos/[documentId]">) {
  const { id, documentId } = await params;
  if (!/^\d{1,10}$/.test(id) || !/^\d{1,10}$/.test(documentId)) return new Response("No encontrado", { status: 404 });
  return downloadFromApi(`/ConnectionRequest/${id}/documents/${documentId}/content`);
}
