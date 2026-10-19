import type { Metadata } from "next";
import Link from "next/link";

import { PublicShell } from "@/components/layout/shells";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { PUBLIC_NAV } from "@/config/navigation";
import { PublicSessionLink } from "@/features/auth";

export const metadata: Metadata = { title: "Página no encontrada" };

// Rutas que no existen en ninguna zona. Usa el armazón público para que el usuario tenga el menú a mano.
export default function NotFound() {
  return (
    <PublicShell nav={PUBLIC_NAV} actions={<PublicSessionLink />}>
      <div className="grid max-w-xl gap-6">
        <PageHeader title="No encontramos esta página" description="Puede que el enlace esté incompleto o que la página se haya movido." />
        <Button asChild className="justify-self-start">
          <Link href="/">Ir al mapa</Link>
        </Button>
      </div>
    </PublicShell>
  );
}
