"use client";

import type { ExpressionSpecification, GeoJSONSource, MapLayerMouseEvent } from "maplibre-gl";
import { useEffect, useRef } from "react";

import { useMap } from "@/components/ui/map";
import { MAP_LABEL_FONT } from "@/config/map";

import type { MapFeatures } from "../lib/geojson";

const SOURCE = "em-transformadores";
const LAYERS = {
  clusters: "em-grupos",
  clusterCount: "em-grupos-cantidad",
  points: "em-transformadores",
  saturatedMark: "em-saturada-marca",
  selected: "em-seleccionado",
} as const;

/** Colores del Design System (globals.css), leídos en el tema activo: así el mapa sigue al modo claro u oscuro. */
function tokens() {
  const css = getComputedStyle(document.documentElement);
  const read = (name: string) => css.getPropertyValue(`--${name}`).trim() || "#888888";
  return {
    available: read("status-available"),
    conditioned: read("status-conditioned"),
    critical: read("status-critical"),
    saturated: read("status-saturated"),
    maintenance: read("status-maintenance"),
    primary: read("primary"),
    primaryForeground: read("primary-foreground"),
    card: read("card"),
    foreground: read("foreground"),
  };
}

type Props = {
  features: MapFeatures;
  selectedId: number | null;
  onSelect: (id: number) => void;
};

/**
 * Capas de transformadores sobre el mapa de mapcn. Una sola fuente GeoJSON y capas WebGL: aguanta los 10.000
 * transformadores de RNF-02 (no son marcadores del DOM). El estado se dibuja con color y con forma (WCAG 1.4.1).
 */
export function TransformerLayers({ features, selectedId, onSelect }: Props) {
  const { map, isLoaded, resolvedTheme } = useMap();
  const onSelectRef = useRef(onSelect);
  useEffect(() => {
    onSelectRef.current = onSelect;
  }, [onSelect]);

  // Crea la fuente y las capas. Se repite tras cambiar de tema, porque el cambio de estilo las borra.
  useEffect(() => {
    if (!map || !isLoaded) return;
    const c = tokens();
    const status = (name: string): ExpressionSpecification => ["==", ["get", "status"], name];
    const merged: ExpressionSpecification = ["has", "point_count"];
    // Un grupo es lo que unió el navegador o lo que ya venía agrupado del servidor.
    const isCluster: ExpressionSpecification = ["any", merged, ["==", ["get", "kind"], "cluster"]];
    const isPoint: ExpressionSpecification = ["all", ["!", merged], ["==", ["get", "kind"], "point"]];
    const count: ExpressionSpecification = ["get", "count"];
    // "2.7 k" a partir de mil, como Intl compact en es-DO.
    const label: ExpressionSpecification = [
      "case",
      [">=", count, 1000],
      ["concat", ["to-string", ["/", ["round", ["/", count, 100]], 10]], " k"],
      ["to-string", count],
    ];

    // Agrupación en el navegador sobre lo que manda el servidor: une grupos y puntos que quedarían encimados
    // (las celdas vecinas del servidor pueden tener centros muy cercanos) y suma sus cantidades.
    const isServerGroup: ExpressionSpecification = ["==", ["get", "kind"], "cluster"];
    const isRisky: ExpressionSpecification = ["in", ["get", "status"], ["literal", ["Critical", "Saturated"]]];
    if (!map.getSource(SOURCE)) {
      map.addSource(SOURCE, {
        type: "geojson",
        data: { type: "FeatureCollection", features: [] },
        cluster: true,
        clusterRadius: 48,
        // Desde zoom 16 cada transformador va suelto.
        clusterMaxZoom: 15,
        clusterProperties: {
          count: ["+", ["case", isServerGroup, ["get", "count"], 1]],
          risky: ["+", ["case", isServerGroup, ["get", "risky"], ["case", isRisky, 1, 0]]],
        },
      });
    }

    if (!map.getLayer(LAYERS.clusters)) {
      map.addLayer({
        id: LAYERS.clusters,
        type: "circle",
        source: SOURCE,
        filter: isCluster,
        paint: {
          "circle-color": c.primary,
          "circle-opacity": 0.9,
          "circle-radius": ["step", count, 15, 50, 19, 500, 24, 2000, 29],
          // Anillo rojo si el grupo incluye transformadores Críticos o Saturados.
          "circle-stroke-color": ["case", [">", ["get", "risky"], 0], c.critical, c.card],
          "circle-stroke-width": ["case", [">", ["get", "risky"], 0], 4, 2],
        },
      });
      map.addLayer({
        id: LAYERS.clusterCount,
        type: "symbol",
        source: SOURCE,
        filter: isCluster,
        layout: { "text-field": label, "text-font": MAP_LABEL_FONT, "text-size": 12, "text-allow-overlap": true },
        paint: { "text-color": c.primaryForeground },
      });
      map.addLayer({
        id: LAYERS.points,
        type: "circle",
        source: SOURCE,
        filter: isPoint,
        paint: {
          "circle-radius": ["interpolate", ["linear"], ["zoom"], 8, 5, 14, 8, 18, 11],
          // Crítica es un anillo (relleno del fondo); las demás, círculos llenos.
          "circle-color": [
            "case",
            status("Available"), c.available,
            status("Conditioned"), c.conditioned,
            status("Critical"), c.card,
            status("Saturated"), c.saturated,
            c.maintenance,
          ],
          "circle-stroke-color": ["case", status("Critical"), c.critical, c.card],
          "circle-stroke-width": ["case", status("Critical"), 3.5, 1.5],
        },
      });
      // Saturada además lleva una "×": se distingue de Crítica sin depender del tono de rojo.
      map.addLayer({
        id: LAYERS.saturatedMark,
        type: "symbol",
        source: SOURCE,
        filter: ["all", isPoint, status("Saturated")],
        layout: { "text-field": "×", "text-font": MAP_LABEL_FONT, "text-size": 13, "text-allow-overlap": true },
        paint: { "text-color": c.card },
      });
      map.addLayer({
        id: LAYERS.selected,
        type: "circle",
        source: SOURCE,
        filter: ["all", isPoint, ["==", ["get", "id"], -1]],
        paint: {
          "circle-radius": ["interpolate", ["linear"], ["zoom"], 8, 10, 14, 13, 18, 16],
          "circle-color": "rgba(0,0,0,0)",
          "circle-stroke-color": c.foreground,
          "circle-stroke-width": 3,
        },
      });
    }

    const clickPoint = (e: MapLayerMouseEvent) => {
      const id = e.features?.[0]?.properties?.id;
      if (typeof id === "number") onSelectRef.current(id);
    };
    // Un grupo se abre acercando el mapa sobre él.
    const clickCluster = async (e: MapLayerMouseEvent) => {
      const feature = e.features?.[0];
      if (feature?.geometry.type !== "Point") return;
      const center = feature.geometry.coordinates as [number, number];
      const clusterId = feature.properties?.cluster_id;
      // Grupo del navegador: el zoom justo en que se separa. Grupo del servidor: dos niveles más.
      const zoom =
        typeof clusterId === "number"
          ? await (map.getSource(SOURCE) as GeoJSONSource).getClusterExpansionZoom(clusterId)
          : map.getZoom() + 2;
      map.easeTo({ center, zoom: Math.min(Math.max(zoom, map.getZoom() + 1), 18) });
    };
    const pointer = () => (map.getCanvas().style.cursor = "pointer");
    const reset = () => (map.getCanvas().style.cursor = "");

    map.on("click", LAYERS.points, clickPoint);
    map.on("click", LAYERS.clusters, clickCluster);
    for (const layer of [LAYERS.points, LAYERS.clusters]) {
      map.on("mouseenter", layer, pointer);
      map.on("mouseleave", layer, reset);
    }

    return () => {
      map.off("click", LAYERS.points, clickPoint);
      map.off("click", LAYERS.clusters, clickCluster);
      for (const layer of [LAYERS.points, LAYERS.clusters]) {
        map.off("mouseenter", layer, pointer);
        map.off("mouseleave", layer, reset);
      }
    };
  }, [map, isLoaded, resolvedTheme]);

  // Datos nuevos: solo se reemplazan, sin recrear capas.
  useEffect(() => {
    if (!map || !isLoaded) return;
    (map.getSource(SOURCE) as GeoJSONSource | undefined)?.setData(features);
  }, [map, isLoaded, features, resolvedTheme]);

  useEffect(() => {
    if (!map || !isLoaded || !map.getLayer(LAYERS.selected)) return;
    map.setFilter(LAYERS.selected, ["all", ["!", ["has", "point_count"]], ["==", ["get", "kind"], "point"], ["==", ["get", "id"], selectedId ?? -1]]);
  }, [map, isLoaded, selectedId, resolvedTheme]);

  return null;
}
