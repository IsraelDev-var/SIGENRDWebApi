"use client";

import { Map, MapControls } from "@/components/ui/map";
import { DOMINICAN_REPUBLIC_BOUNDS, INITIAL_FIT_PADDING_PX, MAP_MAX_BOUNDS, MAP_STYLES, MAP_ZOOM_LIMITS } from "@/config/map";

/**
 * Mapa base de República Dominicana. En F0 solo valida mapcn, el worker local y el estilo;
 * las capas de transformadores llegan en F3.
 */
export default function MapView() {
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
      <MapControls showZoom showLocate />
    </Map>
  );
}
