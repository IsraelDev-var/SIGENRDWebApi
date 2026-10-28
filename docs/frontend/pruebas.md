# Frontend: pruebas

Cómo se prueba el frontend, qué cubre cada nivel y las mediciones de cierre de la F9. Los comandos están en
[`frontend/README.md`](../../frontend/README.md); las pruebas del backend, en `tests/SIGENRD.UnitTests`.

## Niveles

| Nivel | Herramienta | Dónde | Qué cubre |
|---|---|---|---|
| Unitarias | Vitest + Testing Library | `src/**/*.test.ts(x)`, 17 archivos | Matriz de permisos 3.2 fila por fila, contraste de los tokens (WCAG AA), CSP, estados, formatos es-DO, esquemas Zod, errores de importación CSV, IP del cliente, verificación del JWT, redirecciones seguras |
| E2E sin API | Playwright + axe | `e2e/*.spec.ts` (sin `.api`) | Páginas públicas y de acceso, Design System en los dos temas, cabeceras de seguridad y CSP. Corren en escritorio y en móvil (Pixel 7 a 360 px, CA-13) |
| E2E contra la API real | Playwright + axe | `e2e/*.api.spec.ts` | Recorridos por rol con la API .NET, PostGIS y 10.000 transformadores sintéticos |

### Proyectos de Playwright

| Proyecto | Archivos | Por qué va así |
|---|---|---|
| `escritorio`, `movil` | `home`, `public-pages`, `auth-pages`, `design-system`, `security` | No necesitan la API; se repiten a 360 px |
| `con-api` | `session`, `map`, `public` | Primer ingreso, sesión, mapa y seguimiento |
| `con-api-solicitudes` | `requests`, `simulations`, `management`, `administration` | Depende de `con-api`: comparte los usuarios de prueba |
| `con-api-cierre` | `account` | Cambia contraseñas y desactiva usuarios: va solo y al final |

### Qué prueba cada archivo E2E

| Archivo | Pruebas | Qué comprueba |
|---|---|---|
| `session.api.spec.ts` | 15 | Cookie HttpOnly, menú según la matriz 3.2, regreso a la página pedida sin redirecciones externas, credenciales sin revelar si la cuenta existe, registro y primer perfil, instaladora pendiente y validada (RN-05), seguimiento (RF-14) |
| `map.api.spec.ts` | 11 | CA-01 (10.000 agrupados) y CA-04 (filtros en la URL), búsqueda por código y coordenadas (RF-04, RF-05), proxy del mapa solo con endpoints públicos, lista accesible paginada, ficha 404 |
| `requests.api.spec.ts` | 8 | CA-05 (borrador y envío), CA-06 (rechaza `.exe`, > 10 MB y un PDF falso), CA-08 (aprobada sin edición y certificado), RN-22, B-11, observaciones y corrección |
| `simulations.api.spec.ts` | 8 | RF-18 a RF-24 con los valores de la API, CA-10 (PDF), RF-22 (comparación), de la simulación a la solicitud, RN-22 |
| `management.api.spec.ts` | 7 | El ingeniero revisa pero no aprueba, la distribuidora aprueba y el rechazo exige motivo, CA-07, capacidad y mantenimiento, importación CSV con errores por línea y nada importado |
| `administration.api.spec.ts` | 7 | RF-27 a RF-30 (indicadores, ranking, zonas críticas, Excel y PDF), RF-40 (auditoría), usuarios internos solo para Admin |
| `design-system.spec.ts` | 7 | Catálogo con axe en tema claro y oscuro |
| `auth-pages.spec.ts` | 6 | Errores asociados a cada campo, política de contraseñas, cookie falsificada sin acceso, enlaces de correo incompletos |
| `account.api.spec.ts` | 5 | RF-32 (campana), RF-09 (editar perfil), cambio de contraseña, B-5 (cuenta desactivada cierra la sesión) |
| `public.api.spec.ts` | 4 | Expediente inexistente sin filtrar datos, límite de consultas por persona, requisitos según el trámite |
| `home.spec.ts` | 3 | Mapa base sin código de terceros, worker de MapLibre propio, sin violaciones de accesibilidad |
| `public-pages.spec.ts` | 2 | Seguimiento en el menú y número de expediente demasiado largo |
| `security.spec.ts` | 2 | Cabeceras, nonce distinto por petición y cero violaciones de la CSP |

Cada archivo que recorre pantallas incluye una prueba con axe; las de portal y gestión la repiten a 360 px.

## Accesibilidad

- axe-core en las pruebas E2E: cero violaciones serias en cada página recorrida.
- Lighthouse (14-oct-2026, build de producción): **100 en accesibilidad** en las 9 pantallas medidas (mapa, lista,
  requisitos, seguimiento, ingresar, simulador, solicitudes del portal, red y reportes de gestión). Meta de la F9: ≥ 90.
- El estado de un transformador nunca se comunica solo por color (forma, icono y texto) y `/transformadores` ofrece el
  mapa como tabla.
- Pendiente: la prueba manual con lector de pantalla (NVDA en Windows) la tiene que hacer una persona.

## Rendimiento

### CA-01: mapa útil en menos de 3 s a 10 Mbps

Medido el 17-oct-2026 con la build de producción (`pnpm build` + `pnpm start`), la API en Development con 10.000
transformadores y Chrome de escritorio. Red limitada a 10 Mbps de bajada, 5 de subida y 40 ms de latencia. Se cuenta
desde la navegación hasta que el mapa está dibujado y el panel anuncia los transformadores del área.

| Carga | Mediana | Las 5 mediciones |
|---|---|---|
| En frío (sin caché) | **2,2 s** | 2.234, 2.135, 2.117, 2.173, 2.361 ms |
| En caliente | **1,4 s** | 2.105, 1.431, 1.386, 1.201, 1.221 ms |

Sin limitar la red, la mediana es de 1,3 s en los dos casos. **CA-01 se cumple.**

### Lighthouse

| Pantalla | Rendimiento | LCP | TBT |
|---|---|---|---|
| `/requisitos` | 97 | 2,6 s | 20 ms |
| `/seguimiento` | 93 | 3,2 s | 30 ms |
| `/ingresar` | 92 | 3,2 s | 130 ms |
| `/transformadores` | 91 | 3,5 s | 60 ms |
| `/portal/simulaciones/nueva` | 91 | 3,3 s | 150 ms |
| `/gestion/transformadores` | 91 | 3,5 s | 60 ms |
| `/gestion/reportes` | 86 | 3,8 s | 200 ms |
| `/portal/solicitudes` | 84 | 2,8 s | 210 ms |
| `/` (mapa) | 47 | 5,5 s | 3.920 ms |

CLS fue 0 en todas. Lighthouse simula un móvil lento (unos 1,6 Mbps y la CPU cuatro veces más lenta), más exigente que
CA-01. **Límite conocido:** en el mapa, el tiempo de bloqueo viene de iniciar MapLibre GL (WebGL), que ya se carga
diferido. Bajarlo exigiría otra estrategia (por ejemplo, una imagen estática del mapa mientras carga); no se hace en la
Fase I porque CA-01 se cumple.

## Cómo repetir las mediciones

Los scripts de medición no se versionan (viven en una carpeta local ignorada). Para repetirlas:

1. Levantar la API de demostración y la build de producción en el puerto 3100 (`.claude/launch.json`: `api` y `web-prod`).
2. CA-01: una prueba de Playwright que limita la red con `Network.emulateNetworkConditions` (CDP) y mide 5 cargas en
   frío y 5 en caliente hasta el lienzo del mapa y el anuncio del panel.
3. Lighthouse: `npx lighthouse http://localhost:3100/<ruta> --output=json`, con la cookie `em_session` para las
   pantallas privadas.
