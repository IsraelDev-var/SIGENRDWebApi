import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TONE_BADGE_CLASS } from "@/config/status";
import { listInstallers, ValidateInstallerForm } from "@/features/management";
import { cn } from "@/lib/utils";
import { ApiError } from "@/server/api-client";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Instaladoras" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

const FILTERS = [
  { value: "Pending", label: "Pendientes" },
  { value: "Validated", label: "Validadas" },
  { value: "", label: "Todas" },
];

/** Validación de empresas instaladoras (RF-10, RN-05): hasta que la distribuidora la valida, no crea solicitudes. */
export default async function InstallersPage({ searchParams }: PageProps<"/gestion/instaladoras">) {
  const session = await requireAction("gestion", "/gestion/instaladoras", "validateInstallers");
  const raw = one((await searchParams).estado);
  const status = raw === undefined ? "Pending" : FILTERS.some((f) => f.value === raw) ? raw : "Pending";

  let companies: Awaited<ReturnType<typeof listInstallers>> | null = null;
  try {
    companies = await listInstallers(session, status || undefined);
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  }

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader title="Empresas instaladoras" description="Revisa los datos de la empresa antes de validarla. Validada, puede crear solicitudes a nombre de sus clientes." />

      <nav aria-label="Filtro por estado" className="flex flex-wrap gap-2">
        {FILTERS.map((f) => (
          <Link
            key={f.label}
            href={`/gestion/instaladoras?estado=${f.value}`}
            aria-current={status === f.value ? "page" : undefined}
            className={cn(
              "rounded-full border px-3 py-1 text-sm",
              status === f.value ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted",
            )}
          >
            {f.label}
          </Link>
        ))}
      </nav>

      {!companies ? (
        <ErrorState title="No se pudo cargar la lista" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : companies.length === 0 ? (
        <EmptyState title={status === "Pending" ? "No hay empresas esperando validación" : "No hay empresas en esta lista"} />
      ) : (
        <div className="rounded-lg border bg-card">
          <Table label="Empresas instaladoras">
            <TableHeader>
              <TableRow>
                <TableHead scope="col">Empresa</TableHead>
                <TableHead scope="col">RNC</TableHead>
                <TableHead scope="col">Contacto</TableHead>
                <TableHead scope="col">Estado</TableHead>
                <TableHead scope="col">
                  <span className="sr-only">Acciones</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {companies.map((c) => {
                const validated = c.validationStatus === "Validated";
                return (
                  <TableRow key={c.id}>
                    <TableCell className="font-medium">{c.tradeName}</TableCell>
                    <TableCell className="font-mono">{c.rnc}</TableCell>
                    <TableCell className="whitespace-normal">
                      {[c.contactEmail, c.contactPhone, c.address].filter(Boolean).join(" · ") || "Sin datos de contacto"}
                    </TableCell>
                    <TableCell>
                      <Badge className={TONE_BADGE_CLASS[validated ? "success" : "warning"]}>{validated ? "Validada" : "Pendiente"}</Badge>
                    </TableCell>
                    <TableCell className="text-right">{!validated && <ValidateInstallerForm id={c.id} name={c.tradeName} />}</TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
