import type { Metadata } from "next";

import { PageHeader } from "@/components/shared/page-header";
import { CustomerProfileForm, getApplicantProfile, InstallerCompanyForm, isInstallerValidated } from "@/features/auth";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Mi perfil" };

function Detail({ label, value }: { label: string; value?: string | null }) {
  return (
    <div className="grid gap-0.5 sm:grid-cols-[12rem_1fr]">
      <dt className="text-sm text-muted-foreground">{label}</dt>
      <dd>{value || "No indicado"}</dd>
    </div>
  );
}

export default async function ProfilePage() {
  const session = await requireZone("portal", "/portal/perfil");
  const applicant = await getApplicantProfile(session);

  if (applicant.kind === "customer" && !applicant.profile) {
    return (
      <div className="grid max-w-xl gap-6">
        <PageHeader title="Completa tu perfil" description="Datos del titular del servicio. Se usan en tus solicitudes de conexión y de medición neta." />
        <CustomerProfileForm defaultName={session.fullName} defaultEmail={session.email} />
      </div>
    );
  }

  if (applicant.kind === "installer" && !applicant.company) {
    return (
      <div className="grid max-w-xl gap-6">
        <PageHeader title="Registra tu empresa" description="Edesur valida cada instaladora antes de que pueda crear solicitudes." />
        <InstallerCompanyForm defaultEmail={session.email} />
      </div>
    );
  }

  // Editar el perfil (RF-09) espera el endpoint de la API: por ahora solo se consulta.
  return (
    <div className="grid max-w-2xl gap-6">
      <PageHeader title="Mi perfil" description="La edición de estos datos llega en una próxima entrega." />
      <dl className="grid gap-3 rounded-lg border bg-card p-4">
        {applicant.kind === "customer" && applicant.profile && (
          <>
            <Detail label="Titular" value={applicant.profile.fullName} />
            <Detail label="Cédula o RNC" value={applicant.profile.nationalIdOrRnc} />
            <Detail label="Tipo" value={applicant.profile.isLegalEntity ? "Persona jurídica" : "Persona física"} />
            <Detail label="Correo" value={applicant.profile.email} />
            <Detail label="Teléfono" value={applicant.profile.phone} />
            <Detail label="Dirección" value={applicant.profile.address} />
          </>
        )}
        {applicant.kind === "installer" && applicant.company && (
          <>
            <Detail label="Empresa" value={applicant.company.tradeName} />
            <Detail label="RNC" value={applicant.company.rnc} />
            <Detail label="Validación" value={isInstallerValidated(applicant.company) ? "Validada por Edesur" : "Pendiente de validación"} />
            <Detail label="Correo" value={applicant.company.contactEmail} />
            <Detail label="Teléfono" value={applicant.company.contactPhone} />
            <Detail label="Dirección" value={applicant.company.address} />
          </>
        )}
      </dl>
    </div>
  );
}
