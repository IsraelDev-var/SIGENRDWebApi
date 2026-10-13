"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { SlidersHorizontal, X } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import { ErrorState } from "@/components/shared/page-states";
import { AvailabilityValue } from "@/components/shared/availability-value";
import { TransformerStatusBadge } from "@/components/shared/transformer-status-badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

import { useMapState } from "../hooks/use-map-state";
import { clustersToFeatures, pointsToFeatures, type MapFeatures } from "../lib/geojson";
import { fetchClusters, fetchPoints, fetchTransformer, type MapPoint } from "../lib/map-api";
import { clusterZoom, dataModeFor, requestBounds } from "../lib/viewport";
import { LazyMapCanvas } from "./lazy-map-view";
import { MapFilters } from "./map-filters";
import { MapLegend } from "./map-legend";
import { MapSearch } from "./map-search";
import { TransformerDetail } from "./transformer-detail";
import type { Viewport } from "./viewport-watcher";

const EMPTY: MapFeatures = { type: "FeatureCollection", features: [] };

const transformers = (n: number) => `${n.toLocaleString("es-DO")} ${n === 1 ? "transformador" : "transformadores"}`;
/** Cuántos transformadores muestra la lista del área antes de pedir que se acerque el mapa. */
const LIST_LIMIT = 100;

/**
 * Mapa público de disponibilidad (RF-01 a RF-05). A escala país pide grupos al servidor (B-2) y desde zoom 12 los
 * transformadores uno a uno. Todo lo que se ve en el mapa también está en el panel, usable con teclado.
 */
export function MapExplorer() {
  const { filters, selectedId, select } = useMapState();
  const [viewport, setViewport] = useState<Viewport | null>(null);
  const [flyTarget, setFlyTarget] = useState<{ latitude: number; longitude: number; key: number } | null>(null);
  const flewToInitial = useRef(false);

  const mode = viewport ? dataModeFor(viewport.zoom) : "clusters";
  const area = viewport ? requestBounds(viewport.bounds) : null;
  const zoomKey = viewport && mode === "clusters" ? clusterZoom(viewport.zoom) : null;

  const data = useQuery({
    queryKey: ["mapa", mode, area, zoomKey, filters],
    queryFn: async ({ signal }) =>
      mode === "clusters"
        ? { mode, clusters: await fetchClusters(area!, zoomKey!, filters, signal) }
        : { mode, points: await fetchPoints(area!, filters, signal) },
    enabled: area !== null,
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  });

  const detail = useQuery({
    queryKey: ["transformador", selectedId],
    queryFn: ({ signal }) => fetchTransformer(selectedId!, signal),
    enabled: selectedId !== null,
    staleTime: 30_000,
  });

  // Un enlace con ?t=42 abre el mapa centrado en ese transformador.
  useEffect(() => {
    if (flewToInitial.current || !detail.data) return;
    flewToInitial.current = true;
    setFlyTarget({ latitude: detail.data.latitude, longitude: detail.data.longitude, key: Date.now() });
  }, [detail.data]);

  const pick = useCallback(
    (point: Pick<MapPoint, "id" | "latitude" | "longitude">) => {
      flewToInitial.current = true;
      select(point.id);
      setFlyTarget({ latitude: point.latitude, longitude: point.longitude, key: Date.now() });
    },
    [select],
  );

  const features = useMemo(() => {
    if (!data.data) return EMPTY;
    return data.data.mode === "clusters" ? clustersToFeatures(data.data.clusters.items) : pointsToFeatures(data.data.points.items);
  }, [data.data]);

  // Lista del área visible (no del área pedida con margen), por código.
  const inView = useMemo(() => {
    if (!viewport || data.data?.mode !== "points") return null;
    const { minLatitude, minLongitude, maxLatitude, maxLongitude } = viewport.bounds;
    return data.data.points.items
      .filter((p) => p.latitude >= minLatitude && p.latitude <= maxLatitude && p.longitude >= minLongitude && p.longitude <= maxLongitude)
      .sort((a, b) => (a.code ?? "").localeCompare(b.code ?? ""));
  }, [viewport, data.data]);

  const summary = useMemo(() => {
    if (data.isError) return null;
    if (!data.data) return "Cargando transformadores…";
    if (data.data.mode === "clusters") {
      const total = data.data.clusters.total;
      return total === 0
        ? "No hay transformadores en esta área con los filtros elegidos."
        : `${transformers(total)} en el área. Acerca el mapa para verlos uno a uno.`;
    }
    const count = inView?.length ?? 0;
    const truncated = data.data.points.truncated ? " Hay más: acerca el mapa o filtra por zona." : "";
    return count === 0 ? "No hay transformadores en esta área con los filtros elegidos." : `${transformers(count)} en el área.${truncated}`;
  }, [data.data, data.isError, inView]);

  return (
    <div className="flex flex-1 flex-col md:h-[calc(100dvh-6rem)] md:min-h-[32rem] md:flex-row">
      <h1 className="sr-only">Mapa de disponibilidad eléctrica</h1>

      <section aria-label="Mapa de República Dominicana" className="relative min-h-[60vh] flex-1 md:min-h-0">
        <div className="absolute inset-0">
          <LazyMapCanvas
            features={features}
            selectedId={selectedId}
            onSelect={(id) => select(id)}
            onViewportChange={setViewport}
            flyTarget={flyTarget}
          />
        </div>
        {/* En el móvil la leyenda tapaba el mapa: ahí va plegada en el panel. */}
        <MapLegend className="absolute bottom-2 left-2 hidden rounded-md border bg-card/95 p-2 shadow-sm md:block" />
      </section>

      <aside aria-label="Buscar, filtrar y detalle" className="grid content-start gap-4 border-t bg-card p-4 md:w-96 md:overflow-y-auto md:border-t-0 md:border-l">
        <MapSearch onPick={pick} />

        <details className="group rounded-md border px-3 py-2 md:open:pb-3" open>
          <summary className="flex cursor-pointer items-center gap-2 text-sm font-medium">
            <SlidersHorizontal aria-hidden="true" className="size-4" />
            Filtros
          </summary>
          <div className="pt-3">
            <MapFilters />
          </div>
        </details>

        <details className="rounded-md border px-3 py-2 md:hidden">
          <summary className="cursor-pointer text-sm font-medium">Leyenda</summary>
          <MapLegend className="pt-3" />
        </details>

        <p role="status" aria-live="polite" className="text-sm text-muted-foreground">
          {summary}
        </p>
        {data.isError && (
          <ErrorState
            title="No se pudo cargar el mapa"
            description="La conexión con el servidor falló."
            action={
              <Button size="sm" variant="outline" onClick={() => data.refetch()}>
                Reintentar
              </Button>
            }
          />
        )}

        {selectedId !== null ? (
          <section aria-label="Transformador seleccionado" className="grid gap-3 rounded-lg border p-4">
            <div className="flex justify-end">
              <Button variant="ghost" size="sm" onClick={() => select(null)}>
                <X aria-hidden="true" />
                Cerrar
              </Button>
            </div>
            {detail.isPending && (
              <div className="grid gap-2" aria-hidden="true">
                <Skeleton className="h-6 w-1/2" />
                <Skeleton className="h-2 w-full" />
                <Skeleton className="h-24 w-full" />
              </div>
            )}
            {detail.isError && <ErrorState title="No pudimos cargar el transformador" description={detail.error.message} />}
            {detail.data && (
              <>
                <TransformerDetail transformer={detail.data} />
                <Link href={`/transformadores/${detail.data.id}`} className="text-sm text-link underline underline-offset-4 hover:no-underline">
                  Ver la ficha completa
                </Link>
              </>
            )}
          </section>
        ) : (
          inView &&
          inView.length > 0 && (
            <section aria-labelledby="lista-area" className="grid gap-2">
              <h2 id="lista-area" className="text-sm font-medium">
                En el área
              </h2>
              <ul className="grid gap-1">
                {inView.slice(0, LIST_LIMIT).map((point) => (
                  <li key={point.id}>
                    <button
                      type="button"
                      onClick={() => pick(point)}
                      className="flex w-full items-center justify-between gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-secondary focus-visible:outline-2 focus-visible:outline-ring"
                    >
                      <span className="font-mono">{point.code}</span>
                      <span className="flex items-center gap-2">
                        <AvailabilityValue pct={point.availabilityPct} status={point.status} className="text-xs" />
                        <TransformerStatusBadge status={point.status ?? ""} />
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
              {inView.length > LIST_LIMIT && (
                <p className="text-xs text-muted-foreground">Se muestran {LIST_LIMIT} de {inView.length}. Acerca el mapa para ver el resto.</p>
              )}
            </section>
          )
        )}

        <p className="text-sm">
          <Link href="/transformadores" className="text-link underline underline-offset-4 hover:no-underline">
            Ver todos los transformadores como lista
          </Link>
        </p>
      </aside>
    </div>
  );
}
