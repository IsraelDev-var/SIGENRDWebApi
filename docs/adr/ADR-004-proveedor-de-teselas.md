# ADR-004: proveedor del mapa base (teselas)

**Estado:** propuesta, pendiente de decisión (D-2) · **Fases:** F3, F9

## Contexto

En desarrollo el mapa usa los estilos gratuitos de CARTO (`basemaps.cartocdn.com`). Su uso gratuito tiene límites y condiciones que hay que revisar para un uso institucional.

## Opciones

| Opción | Ventajas | Desventajas |
|---|---|---|
| **PMTiles de República Dominicana (Protomaps) en Azure Blob** (recomendada) | Sin cuota ni llave; un archivo estático; mismo origen controlado | Hay que generar y actualizar el archivo y el estilo |
| Proveedor con contrato (MapTiler, Azure Maps) | Sin mantenimiento de teselas | Costo por uso y llave |
| Seguir con CARTO | Nada que hacer | Condiciones para producción por confirmar |

## Qué cambia al decidir

- Las URLs del estilo en `src/config/map.ts` y `components/ui/map.tsx`.
- `MAP_ORIGINS` en `src/config/security.ts` (Content Security Policy).
