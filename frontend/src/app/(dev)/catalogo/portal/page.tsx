import { PortalShell } from "@/components/layout/shells";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/page-states";
import { Button } from "@/components/ui/button";

import { SAMPLE_PORTAL_NAV } from "../sample-nav";

export default function PortalShellDemo() {
  return (
    <PortalShell nav={SAMPLE_PORTAL_NAV} activeHref="/catalogo/portal/solicitudes" actions={<span className="text-sm text-muted-foreground">Cliente de ejemplo</span>}>
      <div className="grid gap-6">
        <PageHeader title="Mis solicitudes" description="Portal de Cliente e Instaladora." actions={<Button>Nueva solicitud</Button>} />
        <EmptyState title="Todavía no tienes solicitudes" description="Busca un transformador en el mapa para empezar." />
      </div>
    </PortalShell>
  );
}
