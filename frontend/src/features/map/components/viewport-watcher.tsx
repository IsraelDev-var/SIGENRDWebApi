"use client";

import { useEffect, useRef } from "react";

import { useMap } from "@/components/ui/map";

import type { Bounds } from "../lib/viewport";

export type Viewport = { bounds: Bounds; zoom: number };

/** Avisa del área visible al terminar cada movimiento del mapa (no en cada fotograma). */
export function ViewportWatcher({ onChange }: { onChange: (viewport: Viewport) => void }) {
  const { map, isLoaded } = useMap();
  const onChangeRef = useRef(onChange);
  useEffect(() => {
    onChangeRef.current = onChange;
  }, [onChange]);

  useEffect(() => {
    if (!map || !isLoaded) return;
    const emit = () => {
      const b = map.getBounds();
      onChangeRef.current({
        bounds: { minLatitude: b.getSouth(), minLongitude: b.getWest(), maxLatitude: b.getNorth(), maxLongitude: b.getEast() },
        zoom: map.getZoom(),
      });
    };
    emit();
    map.on("moveend", emit);
    return () => {
      map.off("moveend", emit);
    };
  }, [map, isLoaded]);

  return null;
}

/** Centra el mapa en un transformador elegido desde la búsqueda, la lista o un enlace. */
export function FlyTo({ target }: { target: { latitude: number; longitude: number; key: number } | null }) {
  const { map, isLoaded } = useMap();

  useEffect(() => {
    if (!map || !isLoaded || !target) return;
    map.flyTo({ center: [target.longitude, target.latitude], zoom: Math.max(map.getZoom(), 15), essential: true });
  }, [map, isLoaded, target]);

  return null;
}
