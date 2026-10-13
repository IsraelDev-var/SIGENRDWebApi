import type { Metadata } from "next";
import Link from "next/link";

import { ForgotPasswordForm } from "@/features/auth";

import { AuthHeading } from "../auth-heading";

export const metadata: Metadata = { title: "Recuperar contraseña" };

export default function ForgotPasswordPage() {
  return (
    <>
      <AuthHeading title="Recuperar la contraseña">Te enviaremos un enlace para crear una nueva.</AuthHeading>
      <ForgotPasswordForm />
      <p className="mt-6 border-t pt-4 text-sm">
        <Link href="/ingresar" className="text-link underline underline-offset-4 hover:no-underline">
          Volver a ingresar
        </Link>
      </p>
    </>
  );
}
