import { KeyRound, LogOut } from "lucide-react";
import Link from "next/link";

import { Button } from "@/components/ui/button";
import { homeFor, ROLE_LABEL } from "@/config/permissions";
import { getSession } from "@/server/session";

import { logoutAction } from "../actions";

/** Usuario y botón de salir, para las barras de las zonas privadas. */
export async function SessionMenu() {
  const session = await getSession();
  if (!session) return null;

  return (
    <div className="flex items-center gap-3">
      <p className="hidden text-right text-sm leading-tight sm:block">
        <span className="block font-medium">{session.fullName || session.email}</span>
        <span className="text-muted-foreground">{ROLE_LABEL[session.role]}</span>
      </p>
      <Button asChild variant="ghost" size="icon">
        <Link href="/cambiar-contrasena" aria-label="Cambiar contraseña" title="Cambiar contraseña">
          <KeyRound aria-hidden="true" />
        </Link>
      </Button>
      <form action={logoutAction}>
        <Button type="submit" variant="outline" size="sm">
          <LogOut aria-hidden="true" />
          Salir
        </Button>
      </form>
    </div>
  );
}

/** En la zona pública: "Ingresar" o el acceso a la zona del usuario. */
export async function PublicSessionLink() {
  const session = await getSession();

  if (!session) {
    return (
      <Button asChild size="sm">
        <Link href="/ingresar">Ingresar</Link>
      </Button>
    );
  }

  return (
    <Button asChild size="sm" variant="outline">
      <Link href={homeFor(session.role)}>{session.role === "Customer" || session.role === "InstallerCompany" ? "Mi portal" : "Gestión"}</Link>
    </Button>
  );
}
