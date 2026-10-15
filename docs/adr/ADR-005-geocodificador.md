# ADR-005: geocodificador para la búsqueda por dirección

**Estado:** propuesta, pendiente de decisión (D-3) · **Fases:** F3

## Contexto

RF-04 pide buscar por dirección. Hoy el mapa busca por código de transformador y por coordenadas (y "Mi ubicación"); el simulador y el asistente reciben coordenadas.

## Opciones

| Opción | Ventajas | Desventajas |
|---|---|---|
| **Proveedor con contrato detrás del BFF** (MapTiler o Azure Maps) (recomendada) | Calidad y cuota garantizadas; la llave queda en el servidor | Costo por uso |
| Nominatim público | Gratis | Su política no admite uso intensivo en producción |
| Nominatim propio | Sin cuota | Servidor y datos que mantener |

## Qué cambia al decidir

- Un route handler en el BFF que reenvía la búsqueda (la llave no llega al navegador) y la CSP si el proveedor se llama desde el navegador.
- El buscador del mapa, y elegir la ubicación en el mapa dentro del asistente y del simulador.
