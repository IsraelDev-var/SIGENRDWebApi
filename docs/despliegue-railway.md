# Despliegue en Railway

La API y el frontend corren como dos servicios Docker en un mismo proyecto de Railway, conectados al repositorio de GitHub. La base de datos se queda en Neon (PostgreSQL 18 con PostGIS y pg_trgm, migraciones al día). Reemplaza la demo de Render (API) y Vercel (frontend).

Cada servicio lee su configuración del archivo `railway.json` de su carpeta: construye con el Dockerfile, espera al health check antes de pasar el tráfico y reinicia si el proceso cae. Railway redespliega solo cuando un push a `master` toca los archivos de ese servicio (`watchPatterns`): un cambio en `frontend/` no reconstruye la API, y al revés.

## 1. Crear el proyecto

1. En railway.com: *New Project → Deploy from GitHub repo* → `SIGENRDWebApi`. Railway crea un servicio; renómbralo `api`.
2. *+ New → GitHub Repo* → el mismo repositorio otra vez; renómbralo `web`.
3. En cada servicio, *Settings*:

| Ajuste | `api` | `web` |
|---|---|---|
| Root Directory | (vacío) | `frontend` |
| Config file path | `/railway.json` | `/frontend/railway.json` (la ruta va completa: no sigue al Root Directory) |
| Branch | `master` | `master` |
| Networking → Public domain | *Generate Domain*, puerto `8080` | *Generate Domain*, puerto `8080` |

Los nombres `api` y `web` importan: las variables de abajo los usan para referirse uno al otro.

## 2. Variables de la API (`api`)

Los valores secretos son los mismos de la demo de Render (`C:\Users\israe\.energymap\render.env`, fuera del repositorio). Se pegan en *Variables → Raw Editor*.

| Variable | Valor |
|---|---|
| `PORT` | `8080` (el puerto de la imagen) |
| `ConnectionStrings__Postgres` | Conexión directa de Neon, formato .NET, sin `-pooler` |
| `ConnectionStrings__IdentityServerDB` | La misma conexión |
| `JwtSettings__PrivateKey` | La llave privada RSA de la demo |
| `TrustedProxy__Key` | La clave compartida con el frontend (16 caracteres o más) |
| `Seed__AdminPassword` | La contraseña del administrador inicial |
| `Storage__Provider` | `Local` (ver *Documentos* abajo) |
| `Storage__LocalPath` | `/data/expedientes` si se monta el volumen; si no, `/tmp/energymap-documentos` (como en Render) |
| `EmailOutbox__DispatcherEnabled` | `false` mientras no haya correo real |
| `Cors__AllowedOrigins__0` | `https://${{web.RAILWAY_PUBLIC_DOMAIN}}` |
| `App__EmailConfirmationUrl` | `https://${{web.RAILWAY_PUBLIC_DOMAIN}}/confirmar-correo` |
| `App__PasswordResetUrl` | `https://${{web.RAILWAY_PUBLIC_DOMAIN}}/restablecer` |

`${{web.RAILWAY_PUBLIC_DOMAIN}}` es una referencia de Railway: se reemplaza por el dominio del servicio `web` y se actualiza sola si cambia. Si después se pone un dominio propio, cambiarlo aquí por ese.

**La API no aplica migraciones al arrancar.** Antes de desplegar una versión con migraciones nuevas hay que aplicarlas en Neon (ver el procedimiento en `docs/despliegue.md`, *Base de datos*).

## 3. Variables del frontend (`web`)

| Variable | Valor |
|---|---|
| `PORT` | `8080` |
| `ENERGYMAP_API_URL` | `http://${{api.RAILWAY_PRIVATE_DOMAIN}}:8080`: el servidor de Next llega a la API por la red privada del proyecto, sin salir a internet |
| `ENERGYMAP_PUBLIC_API_URL` | `https://${{api.RAILWAY_PUBLIC_DOMAIN}}`: la que usa el navegador para subir documentos y la que entra en la CSP |
| `ENERGYMAP_JWT_PUBLIC_KEY` | La llave pública en una sola línea con `\n` (`C:\Users\israe\.energymap\jwt-publica-vercel-una-linea.txt`) |
| `ENERGYMAP_PROXY_KEY` | La misma que `TrustedProxy__Key` de la API |
| `ENERGYMAP_TRUSTED_PROXIES` | `1`: el proxy de Railway agrega la IP del usuario a `X-Forwarded-For`. Confirmarlo en la primera prueba (ver *Comprobar*) |

`NEXT_PUBLIC_MAP_STYLE_LIGHT` y `_DARK` se escriben en el JavaScript al construir. Vacías, el mapa usa CARTO (solo para desarrollo). Si se definen, Railway las pasa como argumento de construcción porque el Dockerfile las declara con `ARG`.

`ENABLE_EXPERIMENTAL_COREPACK` y `frontend/vercel.json` eran solo para Vercel: aquí no hacen falta, porque la imagen instala pnpm con corepack.

## 4. Documentos (opcional, recomendado)

Con `Storage__Provider=Local` los documentos viven en el disco del contenedor y se pierden en cada despliegue. Para conservarlos:

1. En el servicio `api`: *+ New → Volume*, montado en `/data`.
2. `Storage__LocalPath=/data/expedientes`.
3. `RAILWAY_RUN_UID=0`: Railway monta el volumen como root y la imagen corre con un usuario sin privilegios, que no podría escribir en él. Con esta variable el proceso corre como root; es una concesión de la demo. Para producción, Azure Blob (`Storage__Provider=AzureBlob`).

Un volumen limita el servicio a una sola instancia.

## 5. Comprobar

1. `https://<dominio de api>/health` responde `Healthy` (consulta las dos bases en Neon).
2. `https://<dominio de web>/api/salud` responde `Healthy`. Un 503 quiere decir que falta o es inválida una variable de `web`; el log del despliegue dice cuál.
3. La página de inicio carga, lleva la cabecera `Content-Security-Policy` y su `connect-src` incluye el dominio público de la API.
4. `/requisitos` muestra la lista (llega a la API por la red privada).
5. Iniciar sesión con el administrador y subir un PDF a una solicitud (prueba CORS y la subida directa).
6. En los logs de `api`, un inicio de sesión fallido registra la IP real del usuario, no la del frontend (prueba `ENERGYMAP_TRUSTED_PROXIES` y `TrustedProxy__Key`).

## 6. Apagar la demo anterior

Cuando todo lo anterior pase: suspender el servicio de Render y quitar el proyecto de Vercel (o desconectar su despliegue automático), para que no queden dos versiones públicas con la misma base.

## Qué no cubre

- La API recibe directo del navegador solo la subida de documentos. Esa ruta usa la IP del proxy de Railway para el límite por IP, porque la API todavía no lee `ForwardedHeaders` (pendiente conocido).
- Las llaves de Data Protection siguen en el contenedor: los enlaces de confirmación y restablecimiento pendientes dejan de servir tras cada despliegue.
- El CD de GitHub Actions (`cd.yml`) sigue publicando imágenes en GHCR y no despliega en Railway: el despliegue lo hace Railway desde el repositorio.
