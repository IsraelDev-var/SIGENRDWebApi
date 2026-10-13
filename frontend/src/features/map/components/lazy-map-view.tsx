"use client";

import dynamic from "next/dynamic";

/**
 * MapLibre pesa unos 220 KB comprimido: se carga aparte y solo en el navegador, después de pintar la página (CA-01).
 */
const MapView = dynamic(() => import("./map-view"), {
  ssr: false,
  loading: () => (
    <div className="bg-muted flex h-full w-full items-center justify-center text-sm text-muted-foreground">
      Cargando mapa…
    </div>
  ),
});

export function LazyMapView() {
  return <MapView />;
}
