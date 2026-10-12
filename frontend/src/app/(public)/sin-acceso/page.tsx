import type { Metadata } from "next";
import Link from "next/link";

import { ForbiddenState } from "@/components/shared/page-states";
import { Button } from "@/components/ui/button";
import { homeFor } from "@/config/permissions";
import { getSession } from "@/server/session";

export const metadata: Metadata = { title: "Sin acceso" };

/** Una zona que no corresponde al rol (proxy.ts responde aquí con 403). */
export default async function ForbiddenPage() {
  const session = await getSession();

  return (
    <div className="mx-auto grid w-full max-w-xl flex-1 content-center px-4 py-10">
      <h1 className="sr-only">Sin acceso</h1>
      <ForbiddenState
        title="Esta sección no está disponible para tu cuenta"
        description="Cada rol tiene su propia zona: el portal para clientes e instaladoras y la gestión para el personal de Edesur."
        action={
          <Button asChild>
            <Link href={session ? homeFor(session.role) : "/"}>{session ? "Ir a mi inicio" : "Ir al mapa"}</Link>
          </Button>
        }
      />
    </div>
  );
}
