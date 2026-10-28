# Frontend: arquitectura

Arranque y comandos en [`frontend/README.md`](../../frontend/README.md). Decisiones en [`docs/adr/`](../adr/).

## Pila

| Pieza | Elección | Para qué |
|---|---|---|
| Framework | Next.js 16 (App Router), React 19, TypeScript estricto (`noUncheckedIndexedAccess`) | Renderizado en servidor, Server Actions, BFF |
| Estilos | Tailwind CSS 4, tokens en `globals.css` | Tema claro y oscuro, contraste verificado |
| Componentes | shadcn/ui (Radix) | Accesibles por defecto; el código queda en el repo |
| Mapa | mapcn sobre MapLibre GL 6 | WebGL para 10.000 puntos (ADR-002) |
| Gráficos | Recharts 3 (componente `chart` de shadcn) | Reportes RF-27 |
| Datos en el cliente | TanStack Query (mapa y conteo de notificaciones) | Caché y cancelación por área |
| Estado | URL (nuqs y formularios GET) | Enlaces compartibles (ADR-003) |
| Validación | Zod | Mismas reglas que FluentValidation antes de enviar |
| Tipos de la API | openapi-typescript desde `docs/api/openapi-v1.json` | Contrato verificado en CI |

## Zonas y rutas

| Zona | Rutas | Quién |
|---|---|---|
| Pública | `/`, `/transformadores`, `/transformadores/[id]`, `/seguimiento`, `/requisitos` | Cualquiera |
| Acceso | `/ingresar`, `/registro`, `/recuperar`, `/restablecer`, `/confirmar-correo`, `/cambiar-contrasena` | Sin sesión (salvo cambiar contraseña) |
| Portal | `/portal`, `solicitudes` (lista, `nueva`, `[id]`, `[id]/editar`), `simulaciones` (lista, `nueva`, `[id]`, `comparar`), `perfil`, `notificaciones` | Cliente, Instaladora |
| Gestión | `/gestion`, `solicitudes`, `transformadores` (+ `nuevo`, `importar`, `[id]`), `instaladoras`, `reportes`, `zonas-criticas`, `usuarios`, `auditoria`, `notificaciones` | Ingeniero, Distribuidora, Admin según la matriz 3.2 |

Route handlers propios: `/api/mapa/[...ruta]` (proxy de solo lectura del mapa), `/api/notificaciones/conteo`, las descargas de cada zona (`.../documentos/[id]`, `.../certificado`, `.../reporte`, `/gestion/reportes/exportar`) y `/sesion-expirada`.

## Capas del código

```
src/
├── app/          rutas; cada página arma features y valida searchParams
├── features/     auth, map, requests, simulations, management, reports, administration, notifications
│                 (api.ts solo de servidor, actions.ts, components/, schemas y textos; se importan por index.ts)
├── components/   ui/ (shadcn, mapcn), layout/ (shells y menús), shared/ (estados, insignias, campos)
├── server/       sesión, cliente de la API con token, descargas, IP del usuario
├── config/       permisos (matriz 3.2), estados, menús, mapa, seguridad (CSP)
└── lib/          formatos es-DO, utilidades, tipos generados
```

ESLint (`eslint-plugin-boundaries`) impide importar entre capas fuera de lo permitido (tabla en el README).

## Flujo de una página privada

1. `proxy.ts`: pone la CSP con nonce; en `/portal` y `/gestion` lee la cookie y redirige si no hay sesión, si la contraseña es temporal o si el rol no entra.
2. La página llama a `requireZone()` o `requireAction()` (verificación real del JWT y de la acción de la matriz).
3. Pide los datos con `apiData()` (token de la cookie, IP del usuario reenviada). Un 401 cierra la sesión.
4. Las mutaciones son Server Actions que validan con Zod, llaman a la API y revalidan la ruta. La API decide siempre.
