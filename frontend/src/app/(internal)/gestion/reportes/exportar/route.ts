import { can } from "@/config/permissions";
import { periodFrom, periodQuery } from "@/features/reports";
import { downloadFromApi } from "@/server/download";
import { getSession } from "@/server/session";

/** Reporte descargable (RF-30): Excel con el detalle de cada solicitud o PDF resumido. */
export async function GET(request: Request) {
  const session = await getSession();
  if (!session || !can(session.role, "viewReports")) return new Response("No tienes acceso a los reportes.", { status: 403 });

  const url = new URL(request.url);
  const format = url.searchParams.get("formato") === "Pdf" ? "Pdf" : "Xlsx";
  const { period, error } = periodFrom(url.searchParams.get("desde") ?? undefined, url.searchParams.get("hasta") ?? undefined);
  if (error) return new Response(error, { status: 400, headers: { "Content-Type": "text/plain; charset=utf-8" } });

  const query = periodQuery(period);
  query.set("Format", format);
  return downloadFromApi(`/Reports/requests/export?${query}`);
}
