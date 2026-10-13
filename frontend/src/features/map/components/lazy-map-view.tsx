"use client";

import dynamic from "next/dynamic";

import type { MapCanvasProps } from "./map-canvas";

/**
 * MapLibre pesa unos 220 KB comprimido: se carga aparte y solo en el navegador, después de pintar la página (CA-01).
 */
const MapCanvas = dynamic(() => import("./map-canvas"), {
  ssr: false,
  loading: () => (
    <div className="flex h-full w-full items-center justify-center bg-muted text-sm text-muted-foreground">Cargando mapa…</div>
  ),
});

export function LazyMapCanvas(props: MapCanvasProps) {
  return <MapCanvas {...props} />;
}
