"use client";

import { Map, MapControls } from "@/components/ui/map";
import { DOMINICAN_REPUBLIC_BOUNDS, INITIAL_FIT_PADDING_PX, MAP_MAX_BOUNDS, MAP_STYLES, MAP_ZOOM_LIMITS } from "@/config/map";

import type { MapFeatures } from "../lib/geojson";
import { TransformerLayers } from "./transformer-layers";
import { FlyTo, ViewportWatcher, type Viewport } from "./viewport-watcher";

export type MapCanvasProps = {
  features: MapFeatures;
  selectedId: number | null;
  onSelect: (id: number) => void;
  onViewportChange: (viewport: Viewport) => void;
  flyTarget: { latitude: number; longitude: number; key: number } | null;
};

/** El mapa en sí (MapLibre vía mapcn). Se carga aparte porque pesa unos 220 KB comprimido (CA-01). */
export default function MapCanvas({ features, selectedId, onSelect, onViewportChange, flyTarget }: MapCanvasProps) {
  return (
    <Map
      className="h-full w-full"
      styles={MAP_STYLES}
      bounds={DOMINICAN_REPUBLIC_BOUNDS}
      fitBoundsOptions={{ padding: INITIAL_FIT_PADDING_PX }}
      minZoom={MAP_ZOOM_LIMITS.min}
      maxZoom={MAP_ZOOM_LIMITS.max}
      maxBounds={MAP_MAX_BOUNDS}
    >
      <TransformerLayers features={features} selectedId={selectedId} onSelect={onSelect} />
      <ViewportWatcher onChange={onViewportChange} />
      <FlyTo target={flyTarget} />
      <MapControls showZoom showLocate />
    </Map>
  );
}
