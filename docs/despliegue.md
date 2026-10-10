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
| `JwtSettings__Key` | Llave de firma del JWT: 64 bytes aleatorios en Base64 |
| `JwtSettings__Issuer`, `JwtSettings__Audience` | Ya vienen en `appsettings.json`; solo se cambian si hace falta |
| `Seed__AdminPassword` | Contraseña del administrador inicial. Sin ella no se crea |
| `MailSettings__SmtpHost`, `__SmtpPort`, `__SmtpUser`, `__SmtpPass`, `__EmailFrom` | Servidor de correo real |
| `App__PublicBaseUrl` | URL pública para los enlaces de confirmación de correo |

## Base de datos

- PostgreSQL con la extensión **PostGIS**. En Azure Database for PostgreSQL hay que habilitarla en `azure.extensions` antes de la primera migración.
- Los scripts son idempotentes (`migraciones-sql`, artefacto del CD): se pueden aplicar más de una vez. Se ejecutan en orden: `1-persistence.sql` y luego `2-identity.sql`.
- Para aplicarlos a mano: `psql "$URL" -v ON_ERROR_STOP=1 -f 1-persistence.sql`.

## Qué no cubre todavía

- El `/health` solo indica que el proceso responde; no comprueba la base de datos.
- No hay límite de peticiones ni escaneo de la imagen.
- La publicación de imágenes y el despliegue solo se ejecutan en `master`, así que no se pueden probar desde una rama sin activar `workflow_dispatch`.
