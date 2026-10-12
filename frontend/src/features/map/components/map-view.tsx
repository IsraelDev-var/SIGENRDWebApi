"use client";

import { Map, MapControls } from "@/components/ui/map";
import { INITIAL_VIEW, MAP_MAX_BOUNDS, MAP_STYLES, MAP_ZOOM_LIMITS } from "@/config/map";

/**
 * Mapa base de República Dominicana. En F0 solo valida mapcn, el worker local y el estilo;
 * las capas de transformadores llegan en F3.
 */
export default function MapView() {
  return (
    <Map
      className="h-full w-full"
      styles={MAP_STYLES}
      center={INITIAL_VIEW.center}
      zoom={INITIAL_VIEW.zoom}
      minZoom={MAP_ZOOM_LIMITS.min}
      maxZoom={MAP_ZOOM_LIMITS.max}
      maxBounds={MAP_MAX_BOUNDS}
    >
      <MapControls showZoom showLocate />
    </Map>
  );
}
