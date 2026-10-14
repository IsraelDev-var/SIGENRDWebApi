import { Check } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { can, ROLE_LABEL, type Action } from "@/config/permissions";
import { REQUEST_STATUS, type RequestStatus } from "@/config/status";
import { listInstallers, queueCounts } from "@/features/management";
import { ApiError } from "@/server/api-client";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Gestión" };

const STAFF_ACTIONS: { action: Action; label: string }[] = [
  { action: "viewOthersRequests", label: "Consultar las solicitudes" },
  { action: "reviewRequests", label: "Pasar a revisión y emitir observaciones" },
  { action: "decideRequests", label: "Aprobar o rechazar solicitudes" },
  { action: "validateInstallers", label: "Validar empresas instaladoras" },
  { action: "manageCatalogs", label: "Mantener transformadores y zonas" },
  { action: "viewReports", label: "Ver reportes y estadísticas" },
  { action: "manageInternalUsers", label: "Gestionar usuarios internos" },
  { action: "viewAudit", label: "Consultar la auditoría" },
];

// La cola: lo que espera una acción del personal y lo que espera al solicitante.
const QUEUE: RequestStatus[] = ["Received", "UnderReview", "Observed", "InCorrection"];

export default async function InternalHomePage() {
  const session = await requireZone("gestion", "/gestion");
  const allowed = STAFF_ACTIONS.filter(({ action }) => can(session.role, action));

  let counts: Record<string, number> | null = null;
  let pendingInstallers: number | null = null;
  try {
    [counts, pendingInstallers] = await Promise.all([
      queueCounts(session, QUEUE),
      can(session.role, "validateInstallers") ? listInstallers(session, "Pending").then((list) => list.length) : Promise.resolve(null),
    ]);
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  }

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader title="Panel de gestión" description={`${session.fullName || session.email} · ${ROLE_LABEL[session.role]}`} />

      <section aria-labelledby="cola" className="grid gap-3">
        <h2 id="cola" className="font-medium">
          Solicitudes en curso
        </h2>
        {counts ? (
          <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {QUEUE.map((status) => (
              <li key={status}>
                <Link
                  href={`/gestion/solicitudes?estado=${status}`}
                  className="grid gap-1 rounded-lg border bg-card p-4 hover:bg-muted focus-visible:outline-2 focus-visible:outline-ring"
                >
                  <span className="text-sm text-muted-foreground">{REQUEST_STATUS[status].label}</span>
                  <span className="font-mono text-2xl font-semibold tabular-nums">{counts[status]}</span>
                </Link>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-muted-foreground">No se pudieron cargar los contadores. Recarga la página.</p>
        )}
        {pendingInstallers !== null && pendingInstallers > 0 && (
          <p className="text-sm">
            <Link href="/gestion/instaladoras" className="text-link underline underline-offset-4 hover:no-underline">
              {pendingInstallers === 1 ? "1 empresa instaladora espera" : `${pendingInstallers} empresas instaladoras esperan`} validación
            </Link>
          </p>
        )}
      </section>

      <section aria-labelledby="permisos" className="grid gap-3 rounded-lg border bg-card p-4">
        <h2 id="permisos" className="font-medium">
          Con tu rol puedes
        </h2>
        <ul className="grid gap-2 text-sm" data-testid="permisos-del-rol">
          {allowed.map(({ action, label }) => (
            <li key={action} className="flex items-center gap-2">
              <Check aria-hidden="true" className="size-4 text-status-available" />
              {label}
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
