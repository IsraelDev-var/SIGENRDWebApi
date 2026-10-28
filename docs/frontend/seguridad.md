# Frontend: seguridad

Cómo se protege el frontend y el resultado del escaneo de OWASP ZAP del cierre de la F9 (CA-14). La seguridad de la
API está en la auditoría del backend (`AUDITORIA_2026-10-10.md`) y en `SecurityHardeningTests`.

## Controles

| Riesgo | Control | Dónde |
|---|---|---|
| Robo del token desde JavaScript (XSS) | El JWT solo vive en la cookie `em_session`: `HttpOnly`, `Secure`, `SameSite=Lax`, vence con el token. El navegador nunca lo ve | `server/session.ts` (ADR-001) |
| Token falsificado o de otro emisor | Verificación RS256 con la llave pública, emisor y audiencia, en el servidor | `server/token.ts` |
| Acceso a una zona sin permiso | Tres capas: `proxy.ts` (optimista), `requireZone()` / `requireAction()` en cada página con la matriz 3.2, y siempre la API | `config/permissions.ts` |
| Contraseña temporal | El proxy y `requireZone()` llevan a `/cambiar-contrasena`; la API rechaza lo demás | `proxy.ts` |
| Sesión revocada o vencida | Un 401 de la API borra la cookie y lleva a `/sesion-expirada`; aviso 5 minutos antes de vencer | `server/api-client.ts` |
| CSRF | Las mutaciones son Server Actions: Next comprueba que el `Origin` sea el sitio. Además, `SameSite=Lax` y ningún GET modifica datos | Next.js |
| Inyección de scripts | Content Security Policy con nonce por petición y `'strict-dynamic'`, sin `unsafe-inline` ni `unsafe-eval` en producción; React escapa todo el texto (los comentarios de revisión se muestran como texto) | `config/security.ts`, `proxy.ts` |
| Incrustar el sitio en otro (clickjacking) | `frame-ancestors 'none'` y `X-Frame-Options: DENY` | `config/security.ts`, `next.config.ts` |
| Redirección abierta tras el ingreso | `?next=` solo acepta rutas internas | `lib/safe-redirect.ts` |
| Archivos de otra persona | Las descargas pasan por el servidor con el token (la API aplica RN-22); un 403 se muestra como 404; siempre `attachment` y `nosniff` | `server/download.ts` |
| Proxy abierto hacia la API | `/api/mapa/…` solo deja pasar 6 endpoints públicos de lectura y consultas de hasta 1.024 caracteres | `app/api/mapa/[...ruta]/route.ts` |
| Límite por IP inservible detrás del BFF | Se reenvía la IP del usuario con una clave compartida (`ENERGYMAP_PROXY_KEY` = `TrustedProxy:Key`) | `server/client-ip.ts` |
| Código de terceros | Sin CDN: el worker de MapLibre se sirve desde el propio sitio; solo el mapa base viene de fuera (`MAP_ORIGINS` y el origen de `NEXT_PUBLIC_MAP_STYLE_*`) | `scripts/copy-maplibre-worker.mjs` |
| Secretos en el cliente | Solo las variables `NEXT_PUBLIC_*` llegan al navegador, y son URLs de estilos del mapa; el resto se valida al arrancar | `config/env.ts`, `server/env.ts` |

`dangerouslySetInnerHTML` solo aparece en `components/ui/chart.tsx` (shadcn), que escribe variables CSS a partir de la
configuración fija de cada gráfico, nunca de datos del usuario.

### Cabeceras

| Cabecera | Valor |
|---|---|
| `Content-Security-Policy` | `default-src 'self'`; scripts solo con nonce; `object-src 'none'`; `base-uri 'self'`; `form-action 'self'`; `frame-ancestors 'none'` |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | Sin cámara, micrófono, pagos ni USB; ubicación solo del propio sitio |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-origin` (también en la API) |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains`, solo en producción |

`e2e/security.spec.ts` comprueba las cabeceras, que el nonce cambie en cada petición y que el mapa y las páginas
públicas funcionen sin ninguna violación de la CSP.

## OWASP ZAP (CA-14)

Escaneo base (pasivo: araña durante 3 minutos y analiza las respuestas, sin atacar) con ZAP 2.16.1 en Docker, el
17-oct-2026, contra la build de producción del frontend y contra la API en Development.

**Resultado: 0 fallas.** Se corrió dos veces: antes y después de corregir el aviso 90004. Avisos y qué se hizo con cada uno:

| Aviso | Riesgo (confianza) | Decisión |
|---|---|---|
| Insufficient Site Isolation Against Spectre (90004), falta `Cross-Origin-Resource-Policy` | Bajo (media) | **Corregido:** `Cross-Origin-Resource-Policy: same-origin` en el frontend y en la API. En el segundo escaneo ya no aparece |
| Insufficient Site Isolation Against Spectre (90004), falta `Cross-Origin-Embedder-Policy: require-corp` | Bajo (media) | **Riesgo aceptado:** COEP solo hace falta para el aislamiento entre orígenes (`SharedArrayBuffer`), que el sitio no usa, y obligaría al proveedor del mapa base a enviar cabeceras CORP: podría dejar el mapa sin teselas. Ya están COOP y CORP |
| Absence of Anti-CSRF Tokens (10202), formularios de ingreso | Medio (baja) | **Falso positivo aceptado:** son Server Actions; Next rechaza las que no vienen del propio origen y la cookie es `SameSite=Lax`. ZAP solo busca un campo oculto con token |
| CSP: style-src unsafe-inline (10055) | Medio (alta) | **Riesgo aceptado:** MapLibre y Recharts ponen atributos `style`, que no admiten nonce. Los estilos en línea no ejecutan código; los scripts siguen limitados al nonce |
| User Controllable HTML Element Attribute (10031) en `?next=` y `?expediente=` | Informativo | Sin acción: React escapa los atributos y `?next=` pasa por `safeRedirectPath` |
| Content-Type Header Missing (10019) | Informativo | Sin acción: son las redirecciones 307 a `/ingresar`, sin cuerpo |
| Storable and Cacheable Content (10049) | Informativo | Sin acción: los archivos de `_next/static` llevan hash y se cachean un año a propósito; los datos privados van con `no-store` |
| Suspicious Comments (10027), Modern Web Application (10109), Authentication Request Identified (10111) | Informativo | Sin acción: palabras como "query" en el JavaScript minificado; detección del tipo de aplicación |

En la API, el primer escaneo dio el aviso 90004 (corregido); el segundo, solo informativos sobre caché (el catálogo
público se cachea 30 s a propósito, RNF-01) y el aviso de que hay una versión nueva de ZAP.

### Cómo repetirlo

```bash
docker run --rm --add-host=host.docker.internal:host-gateway -v "$PWD:/zap/wrk:rw" zaproxy/zap-stable:2.16.1 \
  zap-baseline.py -t http://host.docker.internal:3100 -m 3 -r zap-frontend.html -I
```

Con el frontend de producción en el puerto 3100 y la API de demostración (`.claude/launch.json`: `web-prod` y `api`).
El escaneo activo (`zap-full-scan.py`) ataca de verdad: se corre solo contra un ambiente de prueba en Azure, nunca
contra producción.

## Pendiente

- Tokens de refresco: hoy la sesión dura 60 minutos sin renovación (ver `docs/pendientes.md`).
- Escaneo activo de ZAP y revisión de la configuración real (HTTPS, `X-Forwarded-For`) en el ambiente de prueba de Azure.
