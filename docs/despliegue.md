# Integración y despliegue continuos

## Qué hace cada flujo

| Flujo | Cuándo corre | Qué hace |
|---|---|---|
| `ci.yml` | En cada push y en cada pull request a `master` | Compila, revisa paquetes vulnerables, corre las pruebas contra PostGIS y smtp4dev reales y comprueba que la imagen Docker compila |
| `cd.yml` | Al hacer push a `master`, al crear una etiqueta `vX.Y.Z` o a mano | Repite el CI, publica la imagen en GitHub Container Registry, genera los scripts SQL de migración y, si está activado, despliega |

El CI se bloquea si hay un paquete con vulnerabilidad conocida o si falla una prueba. El CD no publica nada que no haya pasado el CI.

## Imágenes

`ghcr.io/<usuario>/<repositorio>` en minúsculas, con estas etiquetas:

- `sha-<commit completo>`: la que se despliega.
- `latest`: el último `master`.
- `X.Y.Z`: cuando se crea la etiqueta `vX.Y.Z`.

Para volver a una versión anterior se vuelve a desplegar su etiqueta `sha-...`.

## Activar el despliegue

El despliegue es opcional y está apagado hasta que se configuren estos valores en *Settings → Secrets and variables → Actions*.

**Variables**

| Nombre | Valor |
|---|---|
| `DEPLOY_ENABLED` | `true` |
| `AZURE_WEBAPP_NAME` | Nombre de la Web App para contenedores en Azure App Service |
| `APP_URL` | URL pública, por ejemplo `https://energymap.azurewebsites.net` |

**Secretos**

| Nombre | Valor |
|---|---|
| `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` | Credencial federada (OIDC) de la aplicación de Azure con permiso sobre la Web App |
| `PROD_DATABASE_URL` | Conexión de PostgreSQL con permiso de DDL, en formato `postgresql://usuario:clave@host:5432/base?sslmode=require` |

Recomendado: crear el entorno `production` con *Required reviewers*, para que cada despliegue espere una aprobación.

## Configuración de la aplicación

La imagen no lleva secretos. Se definen como *Application settings* de la Web App (o variables de entorno), con `__` como separador:

| Variable | Para qué |
|---|---|
| `ConnectionStrings__Postgres` | Base de datos del negocio |
| `ConnectionStrings__IdentityServerDB` | Base de datos de Identity |
| `JwtSettings__PrivateKey` | Llave privada RSA de 2048 bits o más que firma los JWT (RS256), en PEM o en Base64 de PKCS#8 (ver abajo). En Azure, como referencia a Key Vault |
| `JwtSettings__Issuer`, `JwtSettings__Audience`, `JwtSettings__DurationInMinutes` | Ya vienen en `appsettings.json` (60 minutos); solo se cambian si hace falta |
| `Seed__AdminPassword` | Contraseña del administrador inicial (10 caracteres o más, con mayúscula, minúscula y número). Sin ella no se crea |
| `MailSettings__SmtpHost`, `__SmtpPort`, `__SmtpUser`, `__SmtpPass`, `__EmailFrom` | Servidor de correo real |
| `App__PublicBaseUrl` | URL pública para los enlaces de confirmación de correo |
| `App__PasswordResetUrl` | Página del frontend que pide la nueva contraseña; recibe `?email=...&code=...`. Por defecto `{App__PublicBaseUrl}/reset-password` |
| `Cors__AllowedOrigins__0`, `__1`, ... | Orígenes del frontend que pueden llamar a la API desde el navegador. Vacío: ninguno |
| `RateLimiting__AuthPermitLimit`, `__PublicTrackingPermitLimit`, `__WindowSeconds` | Peticiones por IP y ventana en `/Auth/*` (10) y en el seguimiento público (30), por minuto |
| `EmailOutbox__DispatcherEnabled`, `__PollIntervalSeconds`, `__MaxAttempts`, `__KeepSentDays` | Bandeja de salida de correo: las peticiones solo guardan el correo; un proceso en segundo plano lo envía y reintenta (1 min, 5 min, 15 min, 1 h, 4 h). Ya vienen en `appsettings.json` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` y relacionadas | Trazas y métricas por OpenTelemetry. Sin ella no se activan. Ver [observabilidad.md](observabilidad.md) |
| `Storage__AzureBlob__ConnectionString` | Cuenta de Azure Storage de los documentos y certificados. Es un secreto (Key Vault). Obligatoria en producción |
| `Storage__AzureBlob__ContainerName` | Contenedor de los expedientes (`expedientes` por defecto). Se crea privado si no existe |
| `Storage__Provider` | `AzureBlob` (por defecto) o `Local`. `Local` es solo para desarrollo: el disco del contenedor se pierde al reiniciar |

**Documentos.** Los archivos van a un contenedor de Blob Storage **privado**: nunca se exponen por URL. Toda descarga pasa por la API, que comprueba en cada una que el usuario pueda ver la solicitud (RN-22). Se aceptan PDF, JPG y PNG de hasta 10 MB, identificados por su contenido; los PDF con JavaScript, acciones de lanzamiento o archivos incrustados se rechazan. Esto no reemplaza un antivirus (ver abajo). Al aprobar una solicitud se genera el certificado en PDF (QuestPDF, licencia Community: gratuita mientras los ingresos anuales de la organización no superen 1 M USD).

**Llave del JWT.** Se genera una vez y se guarda solo en el almacén de secretos:

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 | openssl pkcs8 -topk8 -nocrypt -outform DER | base64 -w0
```

Cambiar la llave invalida todos los tokens emitidos: los usuarios vuelven a iniciar sesión. Cada token lleva en `kid` la huella de la llave que lo firmó.

**Detrás de un proxy.** El límite de peticiones es por IP. Si la API queda detrás de un proxy o balanceador que no entrega la IP real del cliente, todos compartirían el mismo límite: hay que habilitar las cabeceras reenviadas (`ForwardedHeaders`) antes de exponerla.

## Health checks

| Ruta | Qué comprueba | Uso |
|---|---|---|
| `/health` | Que respondan las bases de negocio e Identity (un chequeo por base, 10 s máximo cada uno). `Healthy` o 503 `Unhealthy` | Listo para atender: balanceador y prueba de humo del CD |
| `/health/live` | Solo que el proceso responde | Vivo: reinicio del contenedor |

## Base de datos

- PostgreSQL con las extensiones **PostGIS** y **pg_trgm** (búsqueda parcial por texto). En Azure Database for PostgreSQL hay que habilitar las dos en `azure.extensions` antes de aplicar las migraciones.
- Después de una carga masiva de solicitudes, correr `VACUUM ANALYZE "ConnectionRequests";`: hasta que se procesa, el planificador puede ignorar los índices trigram (autovacuum lo hace solo con el tiempo).
- Los scripts son idempotentes (`migraciones-sql`, artefacto del CD): se pueden aplicar más de una vez. Se ejecutan en orden: `1-persistence.sql` y luego `2-identity.sql`.
- Para aplicarlos a mano: `psql "$URL" -v ON_ERROR_STOP=1 -f 1-persistence.sql`.
- Respaldo y restauración: ver [respaldo.md](respaldo.md).

## Qué no cubre todavía

- No hay escaneo de la imagen ni tokens de refresco (al vencer el JWT se vuelve a iniciar sesión).
- Los documentos subidos no pasan por un antivirus. Para producción conviene activar *Microsoft Defender for Storage* (escaneo de malware al subir) en la cuenta de almacenamiento.
- Plazos de evaluación (20 días) y retiro automático (30 días): pospuestos hasta que Edesur confirme las reglas.
- Las llaves de Data Protection (códigos de confirmación de correo y de restablecimiento) viven en el contenedor: si se reinicia o hay más de una instancia, los enlaces pendientes dejan de servir. Hay que persistirlas antes de escalar.
- Los logs salen como JSON por la salida estándar fuera de desarrollo: la plataforma los recoge. Las alertas recomendadas están en [observabilidad.md](observabilidad.md) y se configuran en la herramienta de monitoreo, no en la API.
- La publicación de imágenes y el despliegue solo se ejecutan en `master`, así que no se pueden probar desde una rama sin activar `workflow_dispatch`.
