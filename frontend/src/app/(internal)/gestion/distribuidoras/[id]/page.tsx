import { CircleCheck } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { PageHeader } from "@/components/shared/page-header";
import { Badge } from "@/components/ui/badge";
import { TONE_BADGE_CLASS } from "@/config/status";
import { DistributorForm, DistributorStatusForm, getManagedDistributor } from "@/features/administration";
import { ApiError } from "@/server/api-client";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Editar distribuidora" };

/** Datos, umbrales y estado de una EDE. */
export default async function DistributorPage({ params, searchParams }: PageProps<"/gestion/distribuidoras/[id]">) {
  const { id } = await params;
  const session = await requireAction("gestion", `/gestion/distribuidoras/${id}`, "manageInternalUsers");
  const numericId = Number(id);
  if (!Number.isInteger(numericId) || numericId <= 0) notFound();

  const query = await searchParams;
  let distributor;
  try {
    distributor = await getManagedDistributor(session, numericId);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) notFound();
    throw error;
  }

  return (
    <div className="grid max-w-3xl grid-cols-1 gap-6">
      <nav aria-label="Ruta" className="text-sm">
        <Link href="/gestion/distribuidoras" className="text-link underline underline-offset-4 hover:no-underline">
          Distribuidoras
        </Link>
      </nav>

      {query.creada === "1" && (
        <p role="status" className="flex items-start gap-2 rounded-md bg-tone-success-soft px-3 py-2 text-sm text-tone-success-text">
          <CircleCheck aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
          Distribuidora creada. Ya puedes darle personal en Usuarios y cargar su red en Transformadores.
        </p>
      )}

      <div className="flex flex-wrap items-center gap-3">
        <PageHeader title={distributor.name} description="Datos de contacto, umbrales de disponibilidad y factor de potencia." />
        <Badge className={TONE_BADGE_CLASS[distributor.isActive ? "success" : "neutral"]}>{distributor.isActive ? "Activa" : "Desactivada"}</Badge>
      </div>

      <section className="rounded-lg border bg-card p-5">
        <DistributorForm key={distributor.id} distributor={distributor} />
      </section>

      <section aria-labelledby="estado" className="grid gap-3 rounded-lg border bg-card p-5">
        <h2 id="estado" className="font-semibold">
          Estado
        </h2>
        <DistributorStatusForm key={`${distributor.id}-${distributor.isActive}`} id={distributor.id} name={distributor.name} active={distributor.isActive} />
      </section>
    </div>
  );
}
