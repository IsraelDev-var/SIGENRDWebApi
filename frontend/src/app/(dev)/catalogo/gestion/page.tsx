import { InternalShell } from "@/components/layout/shells";
import { PageHeader } from "@/components/shared/page-header";
import { RequestStatusBadge } from "@/components/shared/request-status-badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

import { SAMPLE_INTERNAL_NAV } from "../sample-nav";

const COUNTS = [
  { status: "Received", count: 14 },
  { status: "UnderReview", count: 9 },
  { status: "Observed", count: 5 },
];

export default function InternalShellDemo() {
  return (
    <InternalShell nav={SAMPLE_INTERNAL_NAV} activeHref="/catalogo/gestion/solicitudes" actions={<span className="text-sm text-muted-foreground">Distribuidora de ejemplo</span>}>
      <div className="grid gap-6">
        <PageHeader title="Bandeja de revisión" description="Gestión: Ingeniero, Distribuidora y Administrador." />
        <ul className="grid gap-4 sm:grid-cols-3">
          {COUNTS.map(({ status, count }) => (
            <li key={status}>
              <Card>
                <CardHeader>
                  <CardTitle>
                    <RequestStatusBadge status={status} />
                  </CardTitle>
                </CardHeader>
                <CardContent className="font-mono text-3xl font-semibold tabular-nums">{count}</CardContent>
              </Card>
            </li>
          ))}
        </ul>
      </div>
    </InternalShell>
  );
}
