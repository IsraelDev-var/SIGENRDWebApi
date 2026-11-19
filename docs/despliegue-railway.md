# Despliegue en Railway

La API y el frontend corren como dos servicios Docker (`api` y `web`) del proyecto **energymap** de Railway, conectados a la rama `master` del repositorio. La base de datos se queda en Neon (PostgreSQL 18 con PostGIS y pg_trgm, migraciones al día). Reemplaza la demo de Render (API) y Vercel (frontend).

| Servicio | URL pública |
|---|---|
| `web` | https://web-production-a06610a2.up.railway.app |
| `api` | https://api-production-1b1a9313.up.railway.app |

Railway redespliega solo cuando un push a `master` toca los archivos de ese servicio (*watch paths*): un cambio en `frontend/` no reconstruye la API, y al revés.

**Sin `railway.json`.** Railway dejó de aceptar *Config as Code* (`railway.json`) en proyectos nuevos y los archivos existentes dejan de funcionar el 2026-12-01. Los ajustes viven en el servicio (*Settings*) y se pusieron con la CLI y su API GraphQL (`serviceInstanceUpdate`). Si se quieren versionar, el reemplazo es `railway config init` (`.railway/railway.ts`).

## Ajustes de cada servicio

| Ajuste | `api` | `web` |
|---|---|---|
| Source | `IsraelDev-var/SIGENRDWebApi`, rama `master` | El mismo |
| Root Directory | (vacío) | `/frontend` |
| Dockerfile | `Dockerfile` (lo detecta solo) | `Dockerfile` dentro de `frontend` |
| Watch paths | `/SIGENRD*/**`, `/Dockerfile` | `/frontend/**` |
| Health check | `/health/live`, 120 s | `/api/salud`, 120 s |
| Restart | Ante fallo, 5 reintentos | Ante fallo, 5 reintentos |
| Dominio público | Puerto `8080` | Puerto `8080` |
| Volumen | `/data` (documentos) | — |

Los nombres `api` y `web` importan: las variables usan esos nombres para referirse uno al otro.

## Variables de la API (`api`)

Los valores secretos son los mismos de la demo de Render (`C:\Users\israe\.energymap\render.env`, fuera del repositorio).

| Variable | Valor |
|---|---|
| `PORT` | `8080` (el puerto de la imagen) |
| `ConnectionStrings__Postgres` | Conexión directa de Neon, formato .NET, sin `-pooler` |
| `ConnectionStrings__IdentityServerDB` | La misma conexión |
| `JwtSettings__PrivateKey` | La llave privada RSA de la demo |
| `TrustedProxy__Key` | La clave compartida con el frontend (16 caracteres o más) |
| `Seed__AdminPassword` | La contraseña del administrador inicial |
| `Storage__Provider` | `Local` |
| `Storage__LocalPath` | `/data/expedientes` (en el volumen) |
| `RAILWAY_RUN_UID` | `0`: Railway monta el volumen como root y la imagen corre con un usuario sin privilegios, que no podría escribir en él. Es una concesión de la demo; para producción, Azure Blob |
| `EmailOutbox__DispatcherEnabled` | `false` mientras no haya correo real |
| `Cors__AllowedOrigins__0` | `https://${{web.RAILWAY_PUBLIC_DOMAIN}}` |
| `App__EmailConfirmationUrl` | `https://${{web.RAILWAY_PUBLIC_DOMAIN}}/confirmar-correo` |
| `App__PasswordResetUrl` | `https://${{web.RAILWAY_PUBLIC_DOMAIN}}/restablecer` |

`${{web.RAILWAY_PUBLIC_DOMAIN}}` es una referencia de Railway: se reemplaza por el dominio del servicio `web` y se actualiza sola si cambia.

**La API no aplica migraciones al arrancar.** Antes de desplegar una versión con migraciones nuevas hay que aplicarlas en Neon (ver `docs/despliegue.md`, *Base de datos*).

## Variables del frontend (`web`)

| Variable | Valor |
|---|---|
| `PORT` | `8080` |
| `ENERGYMAP_API_URL` | `http://${{api.RAILWAY_PRIVATE_DOMAIN}}:8080`: el servidor de Next llega a la API por la red privada del proyecto |
| `ENERGYMAP_PUBLIC_API_URL` | `https://${{api.RAILWAY_PUBLIC_DOMAIN}}`: la que usa el navegador para subir documentos y la que entra en la CSP |
| `ENERGYMAP_JWT_PUBLIC_KEY` | La llave pública en una sola línea con `\n` |
| `ENERGYMAP_PROXY_KEY` | La misma que `TrustedProxy__Key` de la API |
| `ENERGYMAP_TRUSTED_PROXIES` | `1`: el proxy de Railway agrega la IP del usuario a `X-Forwarded-For` |

`NEXT_PUBLIC_MAP_STYLE_LIGHT` y `_DARK` se escriben en el JavaScript al construir. Vacías, el mapa usa CARTO (solo para desarrollo).

## Cargar las variables de nuevo

Con la CLI (`npm i -g @railway/cli`, `railway login`, `railway link` al proyecto energymap), cada secreto se pasa por la entrada estándar para que no quede en el historial:

```bash
railway variable set JwtSettings__PrivateKey --stdin --skip-deploys -s api < archivo
```

## Comprobar

1. `https://<dominio de api>/health` responde `Healthy` (consulta las dos bases en Neon).
2. `https://<dominio de web>/api/salud` responde `Healthy`. Un 503 quiere decir que falta o es inválida una variable de `web`; el log del despliegue dice cuál.
3. La página de inicio carga, lleva la cabecera `Content-Security-Policy` y su `connect-src` incluye el dominio público de la API.
4. `/requisitos` muestra la lista (llega a la API por la red privada).
5. Iniciar sesión con el administrador y subir un PDF a una solicitud (prueba CORS y la subida directa).
6. En los logs de `api`, un inicio de sesión fallido registra la IP real del usuario, no la del frontend.

## Apagar la demo anterior

Suspender el servicio de Render y quitar el proyecto de Vercel (o desconectar su despliegue automático), para que no queden dos versiones públicas con la misma base.

## Qué no cubre

- La API recibe directo del navegador solo la subida de documentos. Esa ruta usa la IP del proxy de Railway para el límite por IP, porque la API todavía no lee `ForwardedHeaders` (pendiente conocido).
- Las llaves de Data Protection siguen en el contenedor: los enlaces de confirmación y restablecimiento pendientes dejan de servir tras cada despliegue.
- Un volumen limita la API a una sola instancia.
- El CD de GitHub Actions (`cd.yml`) sigue publicando imágenes en GHCR y no despliega en Railway: el despliegue lo hace Railway desde el repositorio.
