import type { Metadata } from "next";
import Link from "next/link";

import { RegisterForm } from "@/features/auth";

import { AuthHeading } from "../auth-heading";

export const metadata: Metadata = { title: "Crear cuenta" };

export default function RegisterPage() {
  return (
    <>
      <AuthHeading title="Crear una cuenta">Las cuentas del personal de Edesur las crea el administrador.</AuthHeading>
      <RegisterForm />
      <p className="mt-6 border-t pt-4 text-sm text-muted-foreground">
        ¿Ya tienes cuenta?{" "}
        <Link href="/ingresar" className="text-link underline underline-offset-4 hover:no-underline">
          Ingresa
        </Link>
      </p>
    </>
  );
}
