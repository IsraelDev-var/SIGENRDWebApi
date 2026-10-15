import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";

import { homeFor } from "@/config/permissions";
import { ChangePasswordForm } from "@/features/auth";
import { getSession } from "@/server/session";

import { AuthHeading } from "../auth-heading";

export const metadata: Metadata = { title: "Cambiar contraseña" };

/** Cambio de contraseña con la sesión iniciada. Con una contraseña temporal es el único lugar al que se puede ir. */
export default async function ChangePasswordPage() {
  const session = await getSession();
  if (!session) redirect("/ingresar?next=/cambiar-contrasena");

  return (
    <>
      <AuthHeading title="Cambiar contraseña">
        {session.mustChangePassword
          ? "Tu cuenta tiene una contraseña temporal que puso un administrador. Elige una propia para continuar."
          : "Escribe tu contraseña actual y la nueva."}
      </AuthHeading>
      <ChangePasswordForm />
      {!session.mustChangePassword && (
        <p className="mt-6 border-t pt-4 text-sm">
          <Link href={homeFor(session.role)} className="text-link underline underline-offset-4 hover:no-underline">
            Volver sin cambiarla
          </Link>
        </p>
      )}
    </>
  );
}
