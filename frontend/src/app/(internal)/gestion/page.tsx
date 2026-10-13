import { Check } from "lucide-react";
import type { Metadata } from "next";

import { PageHeader } from "@/components/shared/page-header";
import { can, ROLE_LABEL, type Action } from "@/config/permissions";
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

export default async function InternalHomePage() {
  const session = await requireZone("gestion", "/gestion");
  const allowed = STAFF_ACTIONS.filter(({ action }) => can(session.role, action));

  return (
    <div className="grid gap-6">
      <PageHeader title="Panel de gestión" description={`${session.fullName || session.email} · ${ROLE_LABEL[session.role]}`} />
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
        <p className="text-sm text-muted-foreground">La bandeja de revisión, los transformadores y los reportes se habilitan en las próximas entregas.</p>
      </section>
    </div>
  );
}
