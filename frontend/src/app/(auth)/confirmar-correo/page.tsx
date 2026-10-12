import type { Metadata } from "next";

import { ErrorState } from "@/components/shared/page-states";
import { ConfirmEmailForm } from "@/features/auth";

import { AuthHeading } from "../auth-heading";

export const metadata: Metadata = { title: "Confirmar correo" };

// Llega desde el correo: /confirmar-correo?userId=…&code=… (App:EmailConfirmationUrl en la API).
export default async function ConfirmEmailPage({ searchParams }: PageProps<"/confirmar-correo">) {
  const { userId, code } = await searchParams;

  if (typeof userId !== "string" || typeof code !== "string" || !userId || !code) {
    return <ErrorState title="El enlace está incompleto" description="Ábrelo de nuevo desde el correo de confirmación." />;
  }

  return (
    <>
      <AuthHeading title="Confirma tu correo">Un paso más para activar tu cuenta.</AuthHeading>
      <ConfirmEmailForm userId={userId} code={code} />
    </>
  );
}
