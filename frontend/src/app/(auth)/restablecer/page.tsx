import type { Metadata } from "next";
import Link from "next/link";

import { ErrorState } from "@/components/shared/page-states";
import { ResetPasswordForm } from "@/features/auth";

import { AuthHeading } from "../auth-heading";

export const metadata: Metadata = { title: "Nueva contraseña" };

// Llega desde el correo: /restablecer?email=…&code=… (App:PasswordResetUrl en la API).
export default async function ResetPasswordPage({ searchParams }: PageProps<"/restablecer">) {
  const { email, code } = await searchParams;

  if (typeof email !== "string" || typeof code !== "string" || !email || !code) {
    return (
      <ErrorState
        title="El enlace está incompleto"
        description="Ábrelo de nuevo desde el correo o pide uno nuevo."
        action={
          <Link href="/recuperar" className="text-link underline underline-offset-4 hover:no-underline">
            Pedir un enlace nuevo
          </Link>
        }
      />
    );
  }

  return (
    <>
      <AuthHeading title="Crear una contraseña nueva" />
      <ResetPasswordForm email={email} code={code} />
    </>
  );
}
