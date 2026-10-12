import type { Metadata } from "next";
import Link from "next/link";

import { LoginForm } from "@/features/auth";
import { safeRedirectPath } from "@/lib/safe-redirect";

import { AuthHeading } from "../auth-heading";

export const metadata: Metadata = { title: "Ingresar" };

export default async function LoginPage({ searchParams }: PageProps<"/ingresar">) {
  const { next } = await searchParams;
  const safeNext = safeRedirectPath(next, "");

  return (
    <>
      <AuthHeading title="Ingresar a EnergyMap">Para dar seguimiento a tus solicitudes y simulaciones.</AuthHeading>
      <LoginForm next={safeNext || undefined} />
      <p className="mt-6 border-t pt-4 text-sm text-muted-foreground">
        ¿No tienes cuenta?{" "}
        <Link href="/registro" className="text-link underline underline-offset-4 hover:no-underline">
          Regístrate
        </Link>
      </p>
    </>
  );
}
