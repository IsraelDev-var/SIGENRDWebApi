import { downloadFromApi } from "@/server/download";

/** Resumen de la simulación en PDF (CA-10). */
export async function GET(_request: Request, { params }: RouteContext<"/portal/simulaciones/[id]/reporte">) {
  const { id } = await params;
  if (!/^\d{1,10}$/.test(id)) return new Response("No encontrado", { status: 404 });
  return downloadFromApi(`/Simulations/${id}/report`);
}
