# Frontend: integración con la API

## Contrato

- Fuente: `docs/api/openapi-v1.json`, generado por la API (`OpenApiContractTests` falla si queda desactualizado).
- Tipos: `pnpm api:types` → `src/lib/api/schema.d.ts`. Los dos archivos van en el mismo commit.
- Enums por nombre (`"UnderReview"`), propiedades no anulables marcadas como requeridas (`RequiredPropertiesSchemaFilter`, que también respeta las referencias opcionales como `holder` o `netMetering`).

## Cliente (`src/server/api-client.ts`)

| Función | Para qué |
|---|---|
| `apiData<T>(ruta, opciones)` | Endpoints con sobre `Response<T>`: devuelve `data` o lanza `ApiError` con el mensaje y los errores de validación |
| `apiRequest<T>` | Respuesta sin sobre (`/Auth/*`) |
| `apiResponse` | Respuesta cruda para reenviar archivos (descargas) |

- `ApiError(status, message, errors)`: 400 con los mensajes de FluentValidation, 403, 404 (también para recursos ajenos, RN-22), 409 (transición de estado inválida, CA-07), 429 (límite) y 5xx con un mensaje genérico.
- `body` puede ser JSON o `FormData` (documentos, CSV): con `FormData` el tiempo de espera es de 120 s.
- Un 401 con token lleva a `/sesion-expirada`.
- Cabeceras `X-EnergyMap-Proxy-Key` y `X-EnergyMap-Client-IP` para que el límite por IP de la API sea por usuario (F4).

## Cambios que se hicieron en la API para el frontend

| Id | Qué | Fase |
|---|---|---|
| B-1 | Catálogos públicos de zonas y distribuidoras | F3 |
| B-2 | `GET /Transformers/clusters` (agregados por zoom) | F3 |
| B-4 | `GET /RequiredDocuments` público | F4 |
| B-7 | Enums como texto | F0 |
| B-8 | Contrato OpenAPI versionado y verificado | F0 |
| B-11 | La instaladora solicita a nombre de un cliente (titular) | F5 |
| D-4 | Estado Borrador: `PUT /{id}`, `POST /{id}/submit` | F5 |
| B-3 | `GET /Transformers/manage` (lista de gestión) | F7 |
| B-5 | `GET /Users`, desactivar y reactivar; token de una cuenta desactivada rechazado | F8 |
| B-6 | `PUT /Customers/me`, `PUT /InstallerCompanies/me` | F9 |
| — | Contraseña temporal: `pwd_change` en el token, `POST /Auth/change-password` | F9 |

## Variables del servidor de Next

| Variable | Qué |
|---|---|
| `ENERGYMAP_API_URL` | URL base de la API |
| `ENERGYMAP_PUBLIC_API_URL` | Opcional. URL de la API vista desde el navegador, para la subida directa de documentos; por defecto `ENERGYMAP_API_URL` |
| `ENERGYMAP_JWT_PUBLIC_KEY`, `ENERGYMAP_JWT_ISSUER`, `ENERGYMAP_JWT_AUDIENCE` | Verificación del JWT (RS256) |
| `ENERGYMAP_PROXY_KEY`, `ENERGYMAP_TRUSTED_PROXIES` | Reenvío de la IP del usuario (= `TrustedProxy:Key` de la API) |
| `ENERGYMAP_DESIGN_CATALOG` | `true` habilita `/catalogo` (solo desarrollo) |
