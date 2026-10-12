import type { ReactNode } from "react";

import { NavLinks } from "@/components/layout/nav-links";
import { CapacityMeter } from "@/components/shared/capacity-meter";
import { KvaValue } from "@/components/shared/kva-value";
import { LastUpdated } from "@/components/shared/last-updated";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState, ForbiddenState } from "@/components/shared/page-states";
import { RequestStatusBadge } from "@/components/shared/request-status-badge";
import { StatusLegend } from "@/components/shared/status-legend";
import { TransformerStatusBadge } from "@/components/shared/transformer-status-badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { REQUEST_STATUS, TRANSFORMER_STATUS, type RequestStatus, type TransformerStatus } from "@/config/status";

import { CATALOG_PAGES } from "./sample-nav";

const DAY_MS = 24 * 60 * 60 * 1000;

// Datos de ejemplo: códigos y valores ficticios, solo para ver los componentes.
const SAMPLE_TRANSFORMERS: { code: string; status: TransformerStatus; pct: number | null; total: number; available: number }[] = [
  { code: "TR-EJEMPLO-01", status: "Available", pct: 82.5, total: 150, available: 123.8 },
  { code: "TR-EJEMPLO-02", status: "Conditioned", pct: 52.0, total: 100, available: 52 },
  { code: "TR-EJEMPLO-03", status: "Critical", pct: 18.0, total: 75, available: 13.5 },
  { code: "TR-EJEMPLO-04", status: "Saturated", pct: -4.0, total: 50, available: -2 },
  { code: "TR-EJEMPLO-05", status: "Maintenance", pct: null, total: 37.5, available: 0 },
];

const SAMPLE_REQUESTS: { code: string; type: string; kva: number; status: RequestStatus; days: number }[] = [
  { code: "EM-2026-000101", type: "Conexión", kva: 10, status: "Received", days: 1 },
  { code: "EM-2026-000102", type: "Medición neta", kva: 12.5, status: "UnderReview", days: 3 },
  { code: "EM-2026-000103", type: "Medición neta", kva: 8, status: "Observed", days: 6 },
  { code: "EM-2026-000104", type: "Conexión", kva: 25, status: "Approved", days: 12 },
];

const TOKENS = [
  ["background", "bg-background"],
  ["card", "bg-card"],
  ["muted", "bg-muted"],
  ["primary", "bg-primary"],
  ["accent", "bg-accent"],
  ["ring", "bg-ring"],
  ["destructive", "bg-destructive"],
  ["border", "bg-border"],
] as const;

function Section({ id, title, children }: { id: string; title: string; children: ReactNode }) {
  return (
    <section aria-labelledby={id} className="grid gap-4">
      <h2 id={id} className="text-lg font-semibold">
        {title}
      </h2>
      {children}
    </section>
  );
}

export default function CatalogPage() {
  const now = new Date();

  return (
    <main id="contenido" className="mx-auto grid w-full max-w-5xl gap-10 px-4 py-8">
      <PageHeader
        title="Catálogo de componentes"
        description="Design System de EnergyMap. Página interna: solo existe en desarrollo y en las pruebas. Los datos son ficticios."
      />
      <nav aria-label="Páginas del catálogo">
        <NavLinks items={CATALOG_PAGES} />
      </nav>

      <Section id="colores" title="Colores">
        <p className="max-w-prose text-sm text-muted-foreground">
          Siguen el tema del sistema operativo. El contraste de cada par texto/fondo se verifica en las pruebas (design-tokens.test.ts).
        </p>
        <ul className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          {TOKENS.map(([name, className]) => (
            <li key={name} className="grid gap-1 text-sm">
              <span className={`h-12 rounded-md border ${className}`} />
              <code className="font-mono text-xs">--{name}</code>
            </li>
          ))}
        </ul>
      </Section>

      <Section id="tipografia" title="Tipografía">
        <div className="grid gap-2">
          <p className="text-2xl font-semibold">Public Sans, títulos e interfaz</p>
          <p className="max-w-prose">
            Texto de párrafo para formularios y explicaciones. La plataforma la usan personas sin formación técnica (RNF-14).
          </p>
          <p className="font-mono tabular-nums">IBM Plex Mono · TR-SD-01234 · 1,250.5 kVA · RD$60,000</p>
        </div>
      </Section>

      <Section id="semaforo" title="Semáforo de disponibilidad">
        <div className="flex flex-wrap gap-2">
          {(Object.keys(TRANSFORMER_STATUS) as TransformerStatus[]).map((status) => (
            <TransformerStatusBadge key={status} status={status} />
          ))}
          <TransformerStatusBadge status="ValorInesperado" />
        </div>
        <div className="grid gap-6 md:grid-cols-2">
          <StatusLegend detailed />
          <div className="grid gap-4">
            {SAMPLE_TRANSFORMERS.map((t) => (
              <CapacityMeter key={t.code} availabilityPct={t.pct} status={t.status} label={t.code} />
            ))}
            <CapacityMeter availabilityPct={null} status="Available" label="Sin dato de la distribuidora" />
          </div>
        </div>
      </Section>

      <Section id="tarjeta" title="Tarjeta de transformador">
        <div className="grid gap-4 sm:grid-cols-2">
          {SAMPLE_TRANSFORMERS.slice(2, 4).map((t, i) => (
            <Card key={t.code}>
              <CardHeader className="flex flex-row items-start justify-between gap-2">
                <CardTitle className="font-mono">{t.code}</CardTitle>
                <TransformerStatusBadge status={t.status} />
              </CardHeader>
              <CardContent className="grid gap-3">
                <CapacityMeter availabilityPct={t.pct} status={t.status} />
                <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
                  <dt className="text-muted-foreground">Capacidad total</dt>
                  <dd className="text-right">
                    <KvaValue value={t.total} />
                  </dd>
                  <dt className="text-muted-foreground">Disponible</dt>
                  <dd className="text-right">
                    <KvaValue value={t.available} />
                  </dd>
                </dl>
                <LastUpdated date={new Date(now.getTime() - (i === 0 ? 5 : 45) * DAY_MS)} now={now} />
              </CardContent>
            </Card>
          ))}
        </div>
      </Section>

      <Section id="solicitudes" title="Estados de solicitud y tablas">
        <div className="flex flex-wrap gap-2">
          {(Object.keys(REQUEST_STATUS) as RequestStatus[]).map((status) => (
            <RequestStatusBadge key={status} status={status} />
          ))}
        </div>
        <div className="rounded-lg border bg-card">
          <Table label="Solicitudes de ejemplo">
            <TableHeader>
              <TableRow>
                <TableHead scope="col">Expediente</TableHead>
                <TableHead scope="col">Tipo</TableHead>
                <TableHead scope="col" className="text-right">
                  Capacidad
                </TableHead>
                <TableHead scope="col">Estado</TableHead>
                <TableHead scope="col" className="text-right">
                  Días
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {SAMPLE_REQUESTS.map((r) => (
                <TableRow key={r.code}>
                  <TableCell className="font-mono">{r.code}</TableCell>
                  <TableCell>{r.type}</TableCell>
                  <TableCell className="text-right">
                    <KvaValue value={r.kva} />
                  </TableCell>
                  <TableCell>
                    <RequestStatusBadge status={r.status} />
                  </TableCell>
                  <TableCell className="text-right tabular-nums">{r.days}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </Section>

      <Section id="acciones" title="Botones y formularios">
        <div className="flex flex-wrap gap-2">
          <Button>Enviar solicitud</Button>
          <Button variant="secondary">Guardar borrador</Button>
          <Button variant="outline">Cancelar</Button>
          <Button variant="destructive">Rechazar</Button>
          <Button variant="link">Ver requisitos</Button>
          <Button disabled>Nueva solicitud</Button>
        </div>
        <form className="grid max-w-md gap-4">
          <div className="grid gap-2">
            <Label htmlFor="catalogo-kva">Capacidad requerida (kVA)</Label>
            <Input id="catalogo-kva" inputMode="decimal" defaultValue="12.5" />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="catalogo-codigo">Código del transformador</Label>
            <Input id="catalogo-codigo" defaultValue="T" aria-invalid="true" aria-describedby="catalogo-codigo-error" />
            <p id="catalogo-codigo-error" className="text-sm text-destructive">
              El código debe tener al menos 2 caracteres.
            </p>
          </div>
        </form>
      </Section>

      <Section id="estados" title="Carga, vacío y error">
        <div className="grid gap-3" aria-hidden="true">
          <Skeleton className="h-6 w-1/3" />
          <Skeleton className="h-4 w-2/3" />
          <Skeleton className="h-24 w-full" />
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          <EmptyState
            title="Todavía no tienes solicitudes"
            description="Busca un transformador en el mapa e inicia una solicitud de conexión o de medición neta."
            action={<Button size="sm">Nueva solicitud</Button>}
          />
          <ErrorState
            title="No se pudo cargar el mapa"
            description="La conexión con el servidor falló. Revisa tu conexión e inténtalo de nuevo."
            action={
              <Button size="sm" variant="outline">
                Reintentar
              </Button>
            }
          />
          <ForbiddenState title="No tienes acceso a este expediente" description="Solo el solicitante y el personal de revisión pueden verlo." />
        </div>
      </Section>
    </main>
  );
}
