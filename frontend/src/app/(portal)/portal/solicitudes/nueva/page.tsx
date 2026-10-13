import { MapPinned } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState, ForbiddenState } from "@/components/shared/page-states";
import { Button } from "@/components/ui/button";
import { getApplicantProfile, isInstallerValidated } from "@/features/auth";
import { emptyRequestForm, getTransformer, RequestWizard } from "@/features/requests";
import { getSimulation } from "@/features/simulations";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Nueva solicitud" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

/**
 * Nueva solicitud sobre un transformador (RN-08): se llega desde el mapa o la ficha del transformador con
 * ?transformador=ID. Antes se comprueba que el solicitante pueda crearla (perfil, RN-05).
 */
export default async function NewRequestPage({ searchParams }: PageProps<"/portal/solicitudes/nueva">) {
  const params = await searchParams;
  const transformerId = one(params.transformador);
  const simulationId = one(params.simulacion);
  const session = await requireZone(
    "portal",
    transformerId ? `/portal/solicitudes/nueva?transformador=${encodeURIComponent(transformerId)}` : "/portal/solicitudes/nueva",
  );
  const applicant = await getApplicantProfile(session);

  const header = <PageHeader title="Nueva solicitud" description="Se guarda como borrador hasta que subas los documentos y la envíes." />;

  if (applicant.kind === "customer" && !applicant.profile) {
    return (
      <div className="grid max-w-2xl gap-6">
        {header}
        <EmptyState
          title="Primero completa tu perfil"
          description="La solicitud lleva los datos del titular del servicio."
          action={
            <Button asChild>
              <Link href="/portal/perfil">Completar perfil</Link>
            </Button>
          }
        />
      </div>
    );
  }

  if (applicant.kind === "installer" && !isInstallerValidated(applicant.company)) {
    return (
      <div className="grid max-w-2xl gap-6">
        {header}
        <ForbiddenState
          title={applicant.company ? "Tu empresa está pendiente de validación" : "Primero registra tu empresa"}
          description="Edesur valida cada instaladora antes de que pueda crear solicitudes (RN-05)."
          action={
            !applicant.company && (
              <Button asChild>
                <Link href="/portal/perfil">Registrar empresa</Link>
              </Button>
            )
          }
        />
      </div>
    );
  }

  if (!transformerId) {
    return (
      <div className="grid max-w-2xl gap-6">
        {header}
        <div className="flex flex-col items-center gap-4 rounded-lg border border-dashed px-6 py-10 text-center">
          <MapPinned aria-hidden="true" className="size-8 text-muted-foreground" />
          <div className="grid gap-1">
            <p className="font-medium">Elige el transformador que alimentará tu proyecto</p>
            <p className="max-w-prose text-sm text-muted-foreground">
              Búscalo en el mapa por tu dirección o en la lista, ábrelo y pulsa «Solicitar conexión». Solo los transformadores
              disponibles, condicionados o críticos aceptan solicitudes.
            </p>
          </div>
          <div className="flex flex-wrap justify-center gap-2">
            <Button asChild>
              <Link href="/">Elegir en el mapa</Link>
            </Button>
            <Button asChild variant="outline">
              <Link href="/transformadores">Ver la lista</Link>
            </Button>
          </div>
        </div>
      </div>
    );
  }

  const transformer = await getTransformer(transformerId);
  if (!transformer) {
    return (
      <div className="grid max-w-2xl gap-6">
        {header}
        <ErrorState
          title="No encontramos ese transformador"
          description="Puede que el enlace esté incompleto. Elígelo de nuevo en el mapa."
          action={
            <Button asChild>
              <Link href="/">Ir al mapa</Link>
            </Button>
          }
        />
      </div>
    );
  }

  if (!transformer.acceptsNewRequests) {
    return (
      <div className="grid max-w-2xl gap-6">
        {header}
        <ForbiddenState
          title={`El transformador ${transformer.code} no acepta solicitudes nuevas`}
          description={
            transformer.status === "Maintenance"
              ? "Está en mantenimiento. Elige otro transformador cercano o inténtalo más adelante."
              : "No tiene capacidad libre (RN-03). Elige otro transformador cercano."
          }
          action={
            <Button asChild>
              <Link href={`/?t=${transformer.id}`}>Ver otros en el mapa</Link>
            </Button>
          }
        />
      </div>
    );
  }

  // Desde el resultado de una simulación: medición neta con su potencia y su ubicación.
  const simulation = simulationId ? await getSimulation(session, simulationId) : null;
  const initialValues = emptyRequestForm(simulation?.latitude != null && simulation.longitude != null ? { latitude: simulation.latitude, longitude: simulation.longitude } : transformer);
  if (simulation) {
    initialValues.requestType = "NetMetering";
    initialValues.installedCapacityKw = String(simulation.installedCapacityKw);
    initialValues.generationSystemType = "Photovoltaic";
  }

  return (
    <div className="grid max-w-3xl gap-6">
      {header}
      {simulation && (
        <p role="status" className="rounded-md bg-tone-info-soft px-3 py-2 text-sm text-tone-info-text">
          Tomamos de la simulación «{simulation.name || `Simulación ${simulation.id}`}» el tipo de trámite, la potencia instalada y la
          ubicación. Revisa el resto de los datos.
        </p>
      )}
      <RequestWizard
        transformer={{
          id: transformer.id,
          code: transformer.code,
          distributorId: transformer.distributorId,
          status: transformer.status,
          availableCapacityKva: transformer.availableCapacityKva,
        }}
        initialValues={initialValues}
        needsHolder={applicant.kind === "installer"}
        holderName={applicant.kind === "customer" ? applicant.profile?.fullName : null}
      />
    </div>
  );
}
