import { downloadFromApi } from "@/server/download";

/** Certificado de aprobación (RN-14). */
export async function GET(_request: Request, { params }: RouteContext<"/gestion/solicitudes/[id]/certificado">) {
  const { id } = await params;
  if (!/^\d{1,10}$/.test(id)) return new Response("No encontrado", { status: 404 });
  return downloadFromApi(`/ConnectionRequest/${id}/certificate`);
}
