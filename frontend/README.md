# EnergyMap · Frontend

Aplicación web de EnergyMap: Next.js (App Router), React, TypeScript estricto, Tailwind CSS, shadcn/ui (Radix) y mapcn sobre MapLibre GL. Consume la API .NET de este mismo repositorio.

## Requisitos

- Node.js 24 o superior.
- pnpm, en la versión de `packageManager` en `package.json`. Sin instalarlo: `npx pnpm@12.9.1 <comando>`, o `corepack enable`.

## Comandos

| Comando | Qué hace |
|---|---|
| `pnpm install` | Instala las dependencias |
| `pnpm dev` | Servidor de desarrollo en http://localhost:3000 |
| `pnpm build` y `pnpm start` | Build de producción y servidor |
| `pnpm lint` | ESLint, incluidos los límites entre capas |
| `pnpm typecheck` | Genera los tipos de rutas y ejecuta `tsc` |
| `pnpm test` | Pruebas unitarias (Vitest) |
| `pnpm e2e` | Pruebas E2E y de accesibilidad (Playwright + axe). La primera vez: `pnpm exec playwright install chromium` |
| `pnpm api:types` | Regenera `src/lib/api/schema.d.ts` desde `../docs/api/openapi-v1.json` |

Si el puerto 3000 está ocupado, `PORT=3100 pnpm e2e`.

## Sesión y permisos

- **BFF:** el navegador nunca ve el JWT. Las Server Actions de `features/auth` llaman a `/api/v1/Auth/*`
  y guardan el token en la cookie `em_session` (HttpOnly, Secure, SameSite=Lax, vence con el token).
  `src/server/session.ts` lo verifica con la llave pública (RS256, emisor y audiencia).
- **Matriz 3.2** en `src/config/permissions.ts`: `can(rol, acción)`, zonas (`/portal` para Cliente e Instaladora,
  `/gestion` para Ingeniero, Distribuidora y Admin) e inicio de cada rol. `permissions.test.ts` la compara fila por fila con el documento.
- **Protección en tres capas:** `src/proxy.ts` (optimista: redirige a `/ingresar` o responde 403 con `/sin-acceso`),
  `requireZone()` en cada página privada y, siempre, la API.
- Cerrar sesión borra la cookie. La API no revoca JWT: el token deja de usarse pero sigue vigente hasta que vence (60 min).
- En la API, `App:EmailConfirmationUrl` y `App:PasswordResetUrl` deben apuntar a `/confirmar-correo` y `/restablecer` de este sitio.

### Pruebas contra la API real

`pnpm e2e` levanta la API si existe `ENERGYMAP_E2E_DB` (servidor PostGIS, sin nombre de base): borra y migra la base
`energymap_e2e`, genera un par de llaves para la ejecución y corre `e2e/*.api.spec.ts` con los usuarios de prueba por rol.
Hace falta `dotnet-ef`. Ejemplo con una base temporal:

```bash
docker run -d --rm --name energymap-test-pg -e POSTGRES_PASSWORD=test-only-pass -p 127.0.0.1:5434:5432 postgis/postgis:17-3.5
ENERGYMAP_E2E_DB="Host=127.0.0.1;Port=5434;Username=postgres;Password=test-only-pass" pnpm e2e
```

## Contrato con la API

`docs/api/openapi-v1.json` es la fuente de los tipos. Si cambia la API:

1. En la raíz del repo, con PostGIS disponible: `ENERGYMAP_UPDATE_OPENAPI=1 dotnet test --filter OpenApiContractTests`.
2. Aquí: `pnpm api:types`.
3. Sube los dos archivos en el mismo commit. El CI falla si alguno quedó desactualizado.

Los enums viajan por nombre (`"Critical"`, no `3`).

## Estructura

```
src/
├── app/            rutas: (public), (auth), (portal), (internal)
├── features/<x>/   módulos de negocio; se importan solo por su index.ts
├── components/     ui/ (shadcn y mapcn), layout/, shared/
├── server/         código solo de servidor (sesión, cliente de la API con token)
├── lib/            utilidades y tipos generados de la API
├── config/         variables de entorno, mapa, permisos
└── hooks/          hooks compartidos
```

### Límites de importación

ESLint (`eslint-plugin-boundaries`) los hace cumplir:

| Desde | Puede importar |
|---|---|
| `app` | `app`, la entrada `index.ts` de cualquier feature, `components`, `server`, `lib`, `config`, `hooks` |
| `features/x` | todo lo de `features/x`, la entrada `index.ts` de otras features, `components`, `server`, `lib`, `config`, `hooks` |
| `components` | `components`, `lib`, `config`, `hooks` |
| `hooks` | `hooks`, `lib`, `config` |
| `server` | `server`, `lib`, `config` |
| `lib`, `config` | `lib`, `config` |

## Design System

- **Tokens** en `src/app/globals.css`, en hex, con tema claro y oscuro según el sistema operativo (D-6).
  `src/config/design-tokens.test.ts` lee ese archivo y comprueba el contraste WCAG 2.1 AA de cada par
  (4.5:1 en texto, 3:1 en bordes, foco y marcadores) en los dos temas. Si cambias un color, corre `pnpm test`.
- **Estados**: `src/config/status.ts` traduce los valores de la API (`"Critical"`, `"UnderReview"`) a etiqueta,
  icono y clases. Crítica y Saturada comparten el rojo pero no el icono. El frontend no calcula estados.
- **Componentes compartidos** en `src/components/shared/`: insignias de estado, medidor de capacidad,
  kVA, fecha de actualización (marca el dato con más de 30 días, umbral provisional D-10), leyenda,
  encabezado de página y estados vacío, error y sin acceso.
- **Layouts** en `src/components/layout/shells.tsx`: `PublicShell`, `PortalShell` e `InternalShell`.
  Los menús son datos (`NavItem`, con el icono por nombre) para poder pasarlos a componentes cliente.
- **Catálogo** en `/catalogo` con `ENERGYMAP_DESIGN_CATALOG=true` (ver `.env.example`). Sin esa variable responde 404.
  Las pruebas E2E lo recorren con axe en los dos temas.
- `src/components/ui/table.tsx` tiene un cambio propio: el contenedor con scroll se puede enfocar con el teclado.

## Mapa

- `src/components/ui/map.tsx` viene de `shadcn add @mapcn/map`, con tres cambios propios marcados al inicio del archivo: el worker se sirve desde `/vendor/maplibre-gl/<versión>/` (no desde unpkg.com), los controles están en español y hay aserciones de índices para `noUncheckedIndexedAccess`. Si se reinstala el componente, hay que repetirlos.
- `pnpm dev` y `pnpm build` copian el worker a `public/vendor/` (no se versiona).
- El estado y el porcentaje de disponibilidad siempre vienen de la API; el frontend no los calcula.
