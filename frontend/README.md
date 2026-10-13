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

## Mapa

- `src/components/ui/map.tsx` viene de `shadcn add @mapcn/map`, con tres cambios propios marcados al inicio del archivo: el worker se sirve desde `/vendor/maplibre-gl/<versión>/` (no desde unpkg.com), los controles están en español y hay aserciones de índices para `noUncheckedIndexedAccess`. Si se reinstala el componente, hay que repetirlos.
- `pnpm dev` y `pnpm build` copian el worker a `public/vendor/` (no se versiona).
- El estado y el porcentaje de disponibilidad siempre vienen de la API; el frontend no los calcula.
