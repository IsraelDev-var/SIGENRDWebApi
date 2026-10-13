import type { components } from "@/lib/api/schema";

import type { MapPoint } from "./map-api";

export type MapCluster = components["schemas"]["TransformerClusterDto"];

/** Propiedades de cada elemento del mapa. "kind" decide la capa en que se dibuja. */
export type FeatureProps =
  | { kind: "point"; id: number; code: string; status: string }
  | { kind: "cluster"; count: number; risky: number };

export type MapFeatures = GeoJSON.FeatureCollection<GeoJSON.Point, FeatureProps>;

const feature = (longitude: number, latitude: number, properties: FeatureProps): GeoJSON.Feature<GeoJSON.Point, FeatureProps> => ({
  type: "Feature",
  geometry: { type: "Point", coordinates: [longitude, latitude] },
  properties,
});

/** Transformadores uno a uno (zoom alto). */
export function pointsToFeatures(points: MapPoint[]): MapFeatures {
  return {
    type: "FeatureCollection",
    features: points.map((p) => feature(p.longitude, p.latitude, { kind: "point", id: p.id, code: p.code ?? "", status: p.status ?? "" })),
  };
}

/** Grupos del servidor. Un grupo de uno se dibuja como el transformador que es. */
export function clustersToFeatures(clusters: MapCluster[]): MapFeatures {
  return {
    type: "FeatureCollection",
    features: clusters.map((c) =>
      c.count === 1 && c.transformerId != null
        ? feature(c.longitude, c.latitude, { kind: "point", id: c.transformerId, code: c.code ?? "", status: c.status ?? "" })
        : feature(c.longitude, c.latitude, {
            kind: "cluster",
            count: c.count,
            // Crítica + Saturada: el grupo lo avisa con un anillo rojo.
            risky: c.critical + c.saturated,
          }),
    ),
  };
}
