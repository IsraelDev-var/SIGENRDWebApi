import { describe, expect, it } from "vitest";

import { clustersToFeatures, pointsToFeatures, type MapCluster } from "./geojson";
import { clusterZoom, contains, dataModeFor, parseCoordinates, POINTS_MIN_ZOOM, requestBounds } from "./viewport";

describe("área a pedir", () => {
  const visible = { minLatitude: 18.4512, minLongitude: -69.9876, maxLatitude: 18.5034, maxLongitude: -69.8911 };

  it("cubre lo visible con margen", () => {
    const area = requestBounds(visible);
    expect(contains(area, visible)).toBe(true);
    expect(area.maxLatitude - area.minLatitude).toBeGreaterThan(visible.maxLatitude - visible.minLatitude);
  });

  it("redondea: movimientos pequeños piden la misma área", () => {
    const moved = { ...visible, minLatitude: visible.minLatitude + 0.0004, maxLatitude: visible.maxLatitude + 0.0004 };
    expect(requestBounds(moved)).toEqual(requestBounds(visible));
  });

  it("no se sale de los límites del mundo", () => {
    const area = requestBounds({ minLatitude: -89.9, minLongitude: -179.9, maxLatitude: 89.9, maxLongitude: 179.9 });
    expect(area).toEqual({ minLatitude: -90, minLongitude: -180, maxLatitude: 90, maxLongitude: 180 });
  });
});

describe("grupos o puntos según el zoom", () => {
  it("a escala país pide grupos y en la ciudad pide puntos", () => {
    expect(dataModeFor(7)).toBe("clusters");
    expect(dataModeFor(POINTS_MIN_ZOOM - 0.01)).toBe("clusters");
    expect(dataModeFor(POINTS_MIN_ZOOM)).toBe("points");
  });

  it("usa el nivel entero de zoom para los grupos", () => {
    expect(clusterZoom(7.8)).toBe(7);
    expect(clusterZoom(-1)).toBe(0);
  });
});

describe("búsqueda por coordenadas", () => {
  it.each([
    ["18.47, -69.90", 18.47, -69.9],
    ["18.47 -69.90", 18.47, -69.9],
    ["  18.4861,-69.9312 ", 18.4861, -69.9312],
    ["18,47; -69,90", 18.47, -69.9],
  ])("entiende %s", (text, latitude, longitude) => {
    expect(parseCoordinates(text)).toEqual({ latitude, longitude });
  });

  it.each(["TR-SD-01234", "18.47", "95, -69", "18.47, -200", "a, b", ""])("no confunde %s con coordenadas", (text) => {
    expect(parseCoordinates(text)).toBeNull();
  });
});

describe("GeoJSON del mapa", () => {
  it("dibuja un grupo de uno como el transformador", () => {
    const single: MapCluster = {
      latitude: 18.5,
      longitude: -69.9,
      count: 1,
      available: 0,
      conditioned: 0,
      critical: 1,
      saturated: 0,
      maintenance: 0,
      transformerId: 42,
      code: "TR-1",
      status: "Critical",
      color: "red",
    };
    const group: MapCluster = { ...single, count: 1250, critical: 3, saturated: 2, transformerId: null, code: null, status: null };

    const [point, cluster] = clustersToFeatures([single, group]).features;

    expect(point!.properties).toEqual({ kind: "point", id: 42, code: "TR-1", status: "Critical" });
    expect(point!.geometry.coordinates).toEqual([-69.9, 18.5]);
    expect(cluster!.properties).toEqual({ kind: "cluster", count: 1250, risky: 5 });
  });

  it("los puntos llevan su id y su estado", () => {
    const features = pointsToFeatures([{ id: 7, code: "TR-7", latitude: 18, longitude: -70, availabilityPct: 40, status: "Conditioned", color: "yellow" }]);
    expect(features.features[0]!.properties).toEqual({ kind: "point", id: 7, code: "TR-7", status: "Conditioned" });
  });
});
