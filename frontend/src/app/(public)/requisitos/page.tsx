import { FileText } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { ErrorState } from "@/components/shared/page-states";
import { StatusLegend } from "@/components/shared/status-legend";
import { Button } from "@/components/ui/button";
import type { components } from "@/lib/api/schema";
import { ApiError, apiData } from "@/server/api-client";

export const metadata: Metadata = {
  title: "Requisitos",
  description: "Documentos que pide Edesur para una conexión nueva o una solicitud de medición neta.",
};

type Requirement = components["schemas"]["DocumentRequirementDto"];

// Cada caso es una solicitud hipotética: la API devuelve lo que pediría la revisión (B-4, misma regla que RN-09 y RN-11).
const SCENARIOS = {
  personal: "requestType=Connection&requestedCapacityKva=5",
  company: "requestType=Connection&requestedCapacityKva=5&isLegalEntity=true",
  large: "requestType=Connection&requestedCapacityKva=11",
  netMeteringOwner: "requestType=NetMetering",
  netMeteringTenant: "requestType=NetMetering&propertyRelation=Tenant",
} as const;

type Loaded = Record<keyof typeof SCENARIOS, Requirement[]>;

async function load(): Promise<Loaded | null> {
  try {
    const entries = await Promise.all(
      Object.entries(SCENARIOS).map(async ([key, query]) => [key, await apiData<Requirement[]>(`/RequiredDocuments?${query}`)] as const),
    );
    return Object.fromEntries(entries) as Loaded;
  } catch (error) {
    if (error instanceof ApiError) return null;
    throw error;
  }
}

/** Lo que pide la segunda lista y no la primera (por ejemplo, lo que se agrega si es una empresa). */
const extra = (all: Requirement[], base: Requirement[]) => all.filter((r) => !base.some((b) => b.code === r.code));

function DocumentList({ items }: { items: Requirement[] }) {
  return (
    <ul className="grid gap-2">
      {items.map((item) => (
        <li key={item.code} className="flex items-start gap-2">
          <FileText aria-hidden="true" className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
          {item.description}
        </li>
      ))}
    </ul>
  );
}

function Section({ id, title, intro, children }: { id: string; title: string; intro: string; children: React.ReactNode }) {
  return (
    <section aria-labelledby={id} className="grid gap-3 rounded-lg border bg-card p-5">
      <h2 id={id} className="text-lg font-semibold">
        {title}
      </h2>
      <p className="text-sm text-muted-foreground">{intro}</p>
      {children}
    </section>
  );
}

/** Requisitos de conexión (§3.3, portal público): qué documentos preparar antes de solicitar. */
export default async function RequirementsPage() {
  const data = await load();

  return (
    <div className="mx-auto grid w-full max-w-3xl gap-6 px-4 py-6">
      <PageHeader
        title="Requisitos para solicitar"
        description="Los documentos que pide la revisión, según el tipo de trámite. Se suben en PDF, JPG o PNG, de hasta 10 MB cada uno."
        actions={
          <Button asChild>
            <Link href="/registro">Crear una cuenta</Link>
          </Button>
        }
      />

      <section aria-labelledby="antes" className="grid gap-3 rounded-lg border bg-card p-5">
        <h2 id="antes" className="text-lg font-semibold">
          Antes de solicitar
        </h2>
        <ol className="grid list-decimal gap-2 pl-5">
          <li>
            Busca en el{" "}
            <Link href="/" className="text-link underline underline-offset-4 hover:no-underline">
              mapa
            </Link>{" "}
            el transformador más cercano y revisa su estado.
          </li>
          <li>Crea tu cuenta como cliente o como empresa instaladora. Las instaladoras deben ser validadas por Edesur.</li>
          <li>Prepara los documentos del trámite que vas a hacer.</li>
        </ol>
        <details className="rounded-md border px-3 py-2">
          <summary className="cursor-pointer text-sm font-medium">Qué significa cada estado del transformador</summary>
          <StatusLegend detailed className="pt-3" />
        </details>
      </section>

      {!data ? (
        <ErrorState title="No se pudieron cargar los requisitos" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : (
        <>
          <Section id="conexion" title="Conexión nueva de hasta 10 kVA" intro="Para una casa o un negocio pequeño.">
            <DocumentList items={data.personal} />
            <h3 className="mt-2 font-medium">Si el solicitante es una empresa o institución, además:</h3>
            <DocumentList items={extra(data.company, data.personal)} />
          </Section>

          <Section id="conexion-mayor" title="Conexión nueva de más de 10 kVA" intro="Exige un proyecto de ingeniería firmado por un ingeniero colegiado.">
            <DocumentList items={data.large} />
          </Section>

          <Section
            id="medicion-neta"
            title="Medición neta (paneles solares u otra generación)"
            intro="Solicitud de evaluación de interconexión, según el Reglamento de Interconexión de Generación Distribuida."
          >
            <DocumentList items={data.netMeteringOwner} />
            {extra(data.netMeteringTenant, data.netMeteringOwner).length > 0 && (
              <>
                <h3 className="mt-2 font-medium">Si no eres el dueño del inmueble, además:</h3>
                <DocumentList items={extra(data.netMeteringTenant, data.netMeteringOwner)} />
              </>
            )}
          </Section>
        </>
      )}

      <p className="text-sm text-muted-foreground">
        Requisitos tomados del reglamento de la CNE (2012) y de los requisitos públicos de las distribuidoras. Edesur puede pedir
        documentos adicionales durante la revisión.
      </p>
    </div>
  );
}
