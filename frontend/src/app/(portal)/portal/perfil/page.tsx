import type { Metadata } from "next";

import { PageHeader } from "@/components/shared/page-header";
import Link from "next/link";

import {
  CustomerProfileForm,
  EditCustomerProfileForm,
  EditInstallerCompanyForm,
  getApplicantProfile,
  InstallerCompanyForm,
  isInstallerValidated,
} from "@/features/auth";
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

  // B-6 (RF-09): nombre y contacto se editan; lo que identifica al titular o a la empresa lo cambia la distribuidora.
  return (
    <div className="grid max-w-2xl gap-6">
      <PageHeader title="Mi perfil" description="Mantén al día tus datos de contacto: Edesur los usa para avisarte de tus trámites." />
      <dl className="grid gap-3 rounded-lg border bg-card p-4">
        {applicant.kind === "customer" && applicant.profile && (
          <>
            <Detail label="Cédula o RNC" value={applicant.profile.nationalIdOrRnc} />
            <Detail label="Tipo" value={applicant.profile.isLegalEntity ? "Persona jurídica" : "Persona física"} />
          </>
        )}
        {applicant.kind === "installer" && applicant.company && (
          <>
            <Detail label="RNC" value={applicant.company.rnc} />
            <Detail label="Validación" value={isInstallerValidated(applicant.company) ? "Validada por Edesur" : "Pendiente de validación"} />
          </>
        )}
      </dl>
      <p className="-mt-4 text-sm text-muted-foreground">Para corregir estos datos, pídelo a Edesur: identifican al titular y deciden los documentos.</p>
      <section aria-labelledby="editar" className="grid gap-4 rounded-lg border bg-card p-4">
        <h2 id="editar" className="font-semibold">
          Datos de contacto
        </h2>
        {applicant.kind === "customer" && applicant.profile && <EditCustomerProfileForm profile={applicant.profile} />}
        {applicant.kind === "installer" && applicant.company && <EditInstallerCompanyForm company={applicant.company} />}
      </section>
      <p className="text-sm">
        <Link href="/cambiar-contrasena" className="text-link underline underline-offset-4 hover:no-underline">
          Cambiar la contraseña
        </Link>
      </p>
    </div>
  );
}
