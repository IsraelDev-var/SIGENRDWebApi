import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatKw, formatPayback, listMySimulations, MAX_COMPARE, ViabilityBadge } from "@/features/simulations";
import { formatDate, formatMoney } from "@/lib/format";
import { ApiError } from "@/server/api-client";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Simulaciones" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

/** Historial de simulaciones (RF-26). Se eligen varias con casillas para compararlas (RF-22): es un formulario GET. */
export default async function SimulationsPage({ searchParams }: PageProps<"/portal/simulaciones">) {
  const session = await requireZone("portal", "/portal/simulaciones");
  const page = Math.max(1, Math.min(10_000, Number(one((await searchParams).pagina)) || 1));

  let result: Awaited<ReturnType<typeof listMySimulations>> | null = null;
  try {
    result = await listMySimulations(session, page);
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  }

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader
        title="Simulaciones"
        description="Estima cuánto ahorrarías con paneles solares y en cuánto tiempo recuperas la inversión. Es informativo: no es una solicitud."
        actions={
          <Button asChild>
            <Link href="/portal/simulaciones/nueva">Nueva simulación</Link>
          </Button>
        }
      />

      {!result ? (
        <ErrorState title="No se pudo cargar el historial" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : result.items.length === 0 ? (
        <EmptyState
          title="Todavía no tienes simulaciones"
          description="Con tu consumo mensual y la potencia que te cotizaron, te decimos si conviene."
          action={
            <Button asChild>
              <Link href="/portal/simulaciones/nueva">Simular ahora</Link>
            </Button>
          }
        />
      ) : (
        <form method="get" action="/portal/simulaciones/comparar" className="grid grid-cols-1 gap-4">
          <p className="text-sm text-muted-foreground">Marca de 2 a {MAX_COMPARE} simulaciones para compararlas.</p>
          <div className="rounded-lg border bg-card">
            <Table label="Mis simulaciones">
              <TableHeader>
                <TableRow>
                  <TableHead scope="col">
                    <span className="sr-only">Comparar</span>
                  </TableHead>
                  <TableHead scope="col">Simulación</TableHead>
                  <TableHead scope="col" className="text-right">
                    Potencia
                  </TableHead>
                  <TableHead scope="col" className="text-right">
                    Ahorro anual
                  </TableHead>
                  <TableHead scope="col" className="text-right">
                    Retorno
                  </TableHead>
                  <TableHead scope="col">Resultado</TableHead>
                  <TableHead scope="col">Fecha</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {result.items.map((s) => {
                  const name = s.name || `Simulación ${s.id}`;
                  return (
                    <TableRow key={s.id}>
                      <TableCell>
                        <input type="checkbox" name="ids" value={s.id} aria-label={`Comparar ${name}`} className="size-4 accent-primary" />
                      </TableCell>
                      <TableCell>
                        <Link href={`/portal/simulaciones/${s.id}`} className="text-link underline underline-offset-4 hover:no-underline">
                          {name}
                        </Link>
                      </TableCell>
                      <TableCell className="text-right font-mono tabular-nums">{formatKw(s.installedCapacityKw)}</TableCell>
                      <TableCell className="text-right font-mono tabular-nums">{formatMoney(s.annualSavings)}</TableCell>
                      <TableCell className="text-right font-mono tabular-nums">{formatPayback(s.paybackYears)}</TableCell>
                      <TableCell>
                        <ViabilityBadge status={s.viabilityStatus} />
                      </TableCell>
                      <TableCell>{formatDate(s.createdAt)}</TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <Button type="submit" variant="outline">
              Comparar las marcadas
            </Button>
            {result.totalPages > 1 && (
              <nav aria-label="Páginas" className="flex items-center gap-2 text-sm">
                {page > 1 && (
                  <Button asChild variant="outline" size="sm">
                    <Link href={`/portal/simulaciones?pagina=${page - 1}`}>Anterior</Link>
                  </Button>
                )}
                <span className="text-muted-foreground">
                  Página {result.page} de {result.totalPages}
                </span>
                {page < result.totalPages && (
                  <Button asChild variant="outline" size="sm">
                    <Link href={`/portal/simulaciones?pagina=${page + 1}`}>Siguiente</Link>
                  </Button>
                )}
              </nav>
            )}
          </div>
        </form>
      )}
    </div>
  );
}
