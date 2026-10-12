import { CircleAlert, Clock } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { getApplicantProfile, isInstallerValidated } from "@/features/auth";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Mi portal" };

export default async function PortalHomePage() {
  const session = await requireZone("portal", "/portal");
  const applicant = await getApplicantProfile(session);
  const isInstaller = applicant.kind === "installer";
  const missingProfile = applicant.kind === "customer" ? !applicant.profile : !applicant.company;
  const pendingValidation = applicant.kind === "installer" && applicant.company !== null && !isInstallerValidated(applicant.company);

  return (
    <div className="grid gap-6">
      <PageHeader title={`Hola, ${session.fullName || "bienvenido"}`} description="Desde aquí darás seguimiento a tus solicitudes y simulaciones." />

      {missingProfile && (
        <section aria-labelledby="perfil-pendiente" className="flex flex-wrap items-start gap-3 rounded-lg bg-tone-warning-soft p-4 text-tone-warning-text">
          <CircleAlert aria-hidden="true" className="mt-0.5 size-5 shrink-0" />
          <div className="grid flex-1 gap-1">
            <h2 id="perfil-pendiente" className="font-medium">
              {isInstaller ? "Registra tu empresa" : "Completa tu perfil"}
            </h2>
            <p className="text-sm">
              {isInstaller
                ? "Necesitamos los datos de la empresa para que Edesur pueda validarla."
                : "Necesitamos los datos del titular antes de crear tu primera solicitud."}
            </p>
          </div>
          <Button asChild size="sm">
            <Link href="/portal/perfil">{isInstaller ? "Registrar empresa" : "Completar perfil"}</Link>
          </Button>
        </section>
      )}

      {pendingValidation && (
        <section aria-labelledby="validacion-pendiente" className="flex items-start gap-3 rounded-lg bg-tone-info-soft p-4 text-tone-info-text">
          <Clock aria-hidden="true" className="mt-0.5 size-5 shrink-0" />
          <div className="grid gap-1">
            <h2 id="validacion-pendiente" className="font-medium">
              Tu empresa está pendiente de validación
            </h2>
            <p className="text-sm">
              Edesur revisa los datos de las instaladoras. Cuando la valide podrás crear solicitudes; mientras tanto puedes consultar el mapa.
            </p>
          </div>
        </section>
      )}

      <section aria-labelledby="proximamente" className="grid gap-2 rounded-lg border bg-card p-4">
        <h2 id="proximamente" className="font-medium">
          Solicitudes y simulaciones
        </h2>
        <p className="text-sm text-muted-foreground">
          Estas secciones se habilitan en las próximas entregas. Por ahora puedes revisar la disponibilidad en el mapa.
        </p>
        <Button asChild variant="outline" size="sm" className="justify-self-start">
          <Link href="/">Ver el mapa</Link>
        </Button>
      </section>
    </div>
  );
}
