import { InternalShell } from "@/components/layout/shells";
import { INTERNAL_NAV, navFor } from "@/config/navigation";
import { SessionMenu } from "@/features/auth";
import { getSession } from "@/server/session";

// Igual que en el portal: la verificación de acceso la hace cada página con requireZone().
export default async function InternalLayout({ children }: { children: React.ReactNode }) {
  const session = await getSession();
  const nav = session ? navFor(INTERNAL_NAV, session.role) : [];

  return (
    <InternalShell nav={nav} actions={<SessionMenu />}>
      {children}
    </InternalShell>
  );
}
