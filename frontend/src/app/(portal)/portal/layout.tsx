import { PortalShell } from "@/components/layout/shells";
import { navFor, PORTAL_NAV } from "@/config/navigation";
import { SessionMenu } from "@/features/auth";
import { getSession } from "@/server/session";

// El layout solo arma la barra; cada página verifica la sesión con requireZone() (los layouts no se
// vuelven a ejecutar al navegar entre páginas).
export default async function PortalLayout({ children }: { children: React.ReactNode }) {
  const session = await getSession();
  const nav = session ? navFor(PORTAL_NAV, session.role) : [];

  return (
    <PortalShell nav={nav} actions={<SessionMenu />}>
      {children}
    </PortalShell>
  );
}
