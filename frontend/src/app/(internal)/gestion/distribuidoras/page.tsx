import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TONE_BADGE_CLASS } from "@/config/status";
import { DistributorForm, listManagedDistributors, type ManagedDistributor } from "@/features/administration";
import { ApiError } from "@/server/api-client";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Distribuidoras" };

const TYPE_LABEL: Record<string, string> = { EDESUR: "Edesur", EDENORTE: "Edenorte", EDEESTE: "Edeeste", OTRA: "Otra" };

const pct = (value: number) => `${value.toLocaleString("es-DO", { maximumFractionDigits: 2 })} %`;

/** Las EDE: alta, edición y desactivación. Cada ingeniero y distribuidora pertenece a una y solo ve lo suyo. */
export default async function DistributorsPage() {
  const session = await requireAction("gestion", "/gestion/distribuidoras", "manageInternalUsers");

  let distributors: ManagedDistributor[] | null = null;
  try {
    distributors = await listManagedDistributors(session);
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  }

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader
        title="Distribuidoras"
        description="Empresas distribuidoras (EDE). Su personal solo ve las solicitudes, la red y los reportes de su EDE; el mapa público muestra todas las activas."
      />

      <details className="rounded-lg border bg-card p-4">
        <summary className="cursor-pointer font-medium">Nueva distribuidora</summary>
        <div className="mt-4">
          <DistributorForm />
        </div>
      </details>

      {!distributors ? (
        <ErrorState title="No se pudo cargar la lista" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : distributors.length === 0 ? (
        <EmptyState title="Todavía no hay distribuidoras" />
      ) : (
        <div className="rounded-lg border bg-card">
          <Table label="Distribuidoras">
            <TableHeader>
              <TableRow>
                <TableHead scope="col">Nombre</TableHead>
                <TableHead scope="col">Tipo</TableHead>
                <TableHead scope="col">Contacto</TableHead>
                <TableHead scope="col">Umbrales</TableHead>
                <TableHead scope="col">Estado</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {distributors.map((d) => (
                <TableRow key={d.id}>
                  <TableCell className="font-medium">
                    <Link href={`/gestion/distribuidoras/${d.id}`} className="text-link underline underline-offset-4 hover:no-underline">
                      {d.name}
                    </Link>
                  </TableCell>
                  <TableCell>{TYPE_LABEL[d.type] ?? d.type}</TableCell>
                  <TableCell className="whitespace-normal">
                    {[d.contactEmail, d.contactPhone].filter(Boolean).join(" · ") || <span className="text-muted-foreground">Sin datos</span>}
                  </TableCell>
                  <TableCell>
                    Disponible ≥ {pct(d.availableThresholdPct)} · Condicionada ≥ {pct(d.conditionedThresholdPct)}
                  </TableCell>
                  <TableCell>
                    <Badge className={TONE_BADGE_CLASS[d.isActive ? "success" : "neutral"]}>{d.isActive ? "Activa" : "Desactivada"}</Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
