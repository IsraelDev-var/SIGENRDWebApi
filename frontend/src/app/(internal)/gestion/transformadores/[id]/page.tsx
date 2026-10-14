import { CircleCheck } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { Button } from "@/components/ui/button";
import { CapacityForm, getTransformerById, MaintenanceForm } from "@/features/management";
import { TransformerDetail } from "@/features/map";
import { requireAction } from "@/server/session";

export async function generateMetadata({ params }: PageProps<"/gestion/transformadores/[id]">): Promise<Metadata> {
  const transformer = await getTransformerById((await params).id);
  return { title: transformer ? `Gestionar ${transformer.code}` : "Transformador no encontrado" };
}

/** Actualizar la capacidad informada y el mantenimiento de un transformador (RN-01, RN-02). */
export default async function ManageTransformerPage({ params, searchParams }: PageProps<"/gestion/transformadores/[id]">) {
  const { id } = await params;
  await requireAction("gestion", `/gestion/transformadores/${id}`, "manageCatalogs");
  const [transformer, query] = await Promise.all([getTransformerById(id), searchParams]);
  if (!transformer) notFound();

  return (
    <div className="grid grid-cols-1 gap-6">
      <nav aria-label="Ruta" className="text-sm">
        <Link href="/gestion/transformadores" className="text-link underline underline-offset-4 hover:no-underline">
          Transformadores
        </Link>
      </nav>

      {query.creado === "1" && (
        <p role="status" className="flex items-start gap-2 rounded-md bg-tone-success-soft px-3 py-2 text-sm text-tone-success-text">
          <CircleCheck aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
          Transformador creado. Ya aparece en el mapa.
        </p>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-lg border bg-card p-5">
          <TransformerDetail transformer={transformer} headingLevel={1} applicantActions={false} />
        </div>

        <div className="grid content-start gap-6">
          <section aria-labelledby="capacidad" className="grid gap-3 rounded-lg border bg-card p-4">
            <h2 id="capacidad" className="font-semibold">
              Capacidad informada
            </h2>
            <p className="text-sm text-muted-foreground">La reservada por solicitudes en revisión no se edita: la lleva el sistema.</p>
            <CapacityForm id={transformer.id} total={transformer.totalCapacityKva} available={transformer.availableCapacityKva} />
          </section>

          <section aria-labelledby="mantenimiento" className="grid gap-3 rounded-lg border bg-card p-4">
            <h2 id="mantenimiento" className="font-semibold">
              Mantenimiento
            </h2>
            <MaintenanceForm id={transformer.id} inMaintenance={transformer.status === "Maintenance"} />
          </section>

          <Button asChild variant="outline" className="justify-self-start">
            <Link href={`/?t=${transformer.id}`}>Ver en el mapa</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
