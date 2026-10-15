# ADR-002: MapLibre GL mediante mapcn

**Estado:** aceptada (2026-10-11) · **Fases:** F0, F3

## Contexto

El mapa es la pantalla principal (RF-01 a RF-05) y debe mostrar hasta 10.000 transformadores con fluidez (RNF-02). El documento de requisitos mencionaba "Leaflet o Mapbox / Mancn" (errata de mapcn, I-1); Mapbox GL tiene licencia propietaria desde la v2.

## Decisión

MapLibre GL (BSD) a través de mapcn (`shadcn add @mapcn/map`), con capas de círculos y símbolos sobre una sola fuente GeoJSON en WebGL, agrupamiento en el servidor a escala país (`/Transformers/clusters`, B-2) y en el cliente al acercarse.

## Alternativas descartadas

- **Leaflet:** maduro y ligero, pero con miles de marcadores en el DOM o canvas se degrada y necesita plugins.
- **Mapbox GL:** licencia y costo por uso.

## Consecuencias

- El código de mapcn queda en el repo (`components/ui/map.tsx`, con tres cambios propios documentados): si mapcn deja de mantenerse se sigue con MapLibre directo.
- El worker se sirve desde nuestro origen (`/vendor/maplibre-gl/...`), lo que permite una CSP sin orígenes de scripts externos.
- El costo está en las teselas: ver ADR-004.
