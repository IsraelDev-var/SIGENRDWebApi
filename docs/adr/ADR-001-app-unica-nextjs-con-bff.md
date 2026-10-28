# ADR-001: una sola aplicación Next.js con BFF y sesión en cookie

**Estado:** aceptada (2026-10-11, propuesta del frontend aprobada) · **Fases:** F0, F2

## Contexto

EnergyMap tiene tres públicos: visitantes del mapa, solicitantes (cliente e instaladora) y personal de la distribuidora. La API .NET emite JWT RS256 de 60 minutos y aplica la matriz de permisos 3.2.

## Decisión

- Una sola app Next.js (App Router) en `frontend/`, con tres zonas: pública, `/portal` y `/gestion`.
- **BFF:** el navegador nunca recibe el JWT. Las Server Actions y los route handlers llaman a la API desde el servidor de Next con el token guardado en la cookie `em_session` (HttpOnly, Secure, SameSite=Lax).
- El mapa pide sus datos a un proxy de solo lectura (`/api/mapa/...`) con una lista cerrada de endpoints públicos.
- Autorización en tres capas: `proxy.ts` (optimista), `requireZone()`/`requireAction()` en cada página y, siempre, la API.

## Alternativas descartadas

- **SPA con el token en el navegador** (localStorage o memoria): expone el token a cualquier XSS.
- **Dos aplicaciones** (público y gestión): duplica el Design System, la sesión y el despliegue para un equipo pequeño.

## Consecuencias

- Las descargas (documentos, certificado, PDF, Excel) pasan por route handlers que añaden el token.
- Las llamadas salen del servidor de Next: la API vería una sola IP. Se reenvía la del usuario con una clave compartida (`TrustedProxy:Key` / `ENERGYMAP_PROXY_KEY`, F4).
- Un token rechazado (vencido o cuenta desactivada) cierra la sesión del navegador (`/sesion-expirada`, F9).
