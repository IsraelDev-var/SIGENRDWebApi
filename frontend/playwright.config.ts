import { generateKeyPairSync } from "node:crypto";
import { tmpdir } from "node:os";
import { join } from "node:path";

import { defineConfig, devices } from "@playwright/test";

// Contraseña de los usuarios de prueba que siembra la API (igual que en e2e/support.ts). Va repetida a propósito:
// con cualquier import relativo en este archivo, Playwright (Windows) termina en silencio al reintentar una prueba.
const E2E_TEST_PASSWORD = "Prueba-Roles-2026!";

const port = Number(process.env.PORT ?? 3000);
const apiPort = Number(process.env.E2E_API_PORT ?? 5199);
const isCI = !!process.env.CI;

/**
 * Servidor PostgreSQL con PostGIS para las pruebas contra la API real, sin nombre de base
 * ("Host=…;Port=…;Username=…;Password=…"). Sin esta variable, las pruebas *.api.spec.ts se omiten.
 */
const e2eDb = process.env.ENERGYMAP_E2E_DB;

// Clave compartida entre Next y la API para reenviar la IP del usuario (TrustedProxyClientIpMiddleware). Solo pruebas.
const E2E_PROXY_KEY = "clave-de-proxy-solo-para-pruebas-e2e";

// Llaves propias de cada ejecución: la API firma con la privada y Next verifica con la pública.
// La configuración se vuelve a evaluar en cada worker, pero los servidores se levantan una sola vez.
const keys = generateKeyPairSync("rsa", { modulusLength: 2048 });
const publicKeyPem = keys.publicKey.export({ type: "spki", format: "pem" }).toString();
const privateKeyBase64 = keys.privateKey.export({ type: "pkcs8", format: "der" }).toString("base64");

const nextServer = (serverPort: number, catalog: boolean) => ({
  // En CI se prueba el build de producción; en local, el servidor de desarrollo.
  command: isCI ? `pnpm start --port ${serverPort}` : `pnpm dev --port ${serverPort}`,
  url: `http://localhost:${serverPort}`,
  reuseExistingServer: !isCI,
  timeout: 120_000,
  env: {
    ENERGYMAP_API_URL: `http://localhost:${apiPort}`,
    ENERGYMAP_JWT_PUBLIC_KEY: publicKeyPem,
    ENERGYMAP_PROXY_KEY: E2E_PROXY_KEY,
    // Sin proxy delante en las pruebas: la IP es la que manda el navegador en X-Forwarded-For (cada prueba usa una).
    ENERGYMAP_TRUSTED_PROXIES: "1",
    // El catálogo del Design System responde 404 sin esta variable.
    ENERGYMAP_DESIGN_CATALOG: catalog ? "true" : "false",
  },
});

const database = `${e2eDb};Database=energymap_e2e`;
const ef = (verb: string, project: string, context: string) =>
  `dotnet ef database ${verb} -p ../${project} -s ../${project} -c ${context}`;

// API .NET en Development (siembra un usuario de prueba por rol) sobre una base nueva en cada ejecución.
const apiServer = e2eDb
  ? [
      {
        command: [
          ef("drop --force", "SIGENRD.Infrastructure.Persistences", "AppContextSIGENRD"),
          ef("update", "SIGENRD.Infrastructure.Persistences", "AppContextSIGENRD"),
          ef("update", "SIGENRD.Infrastructure.Identity", "IdentityDbContextSIGENRD"),
          // E2E_API_CONFIGURATION=Release sirve si ya hay una API local en Debug corriendo (bloquea sus binarios).
          `dotnet run --project ../SIGENRDWebApi --no-launch-profile -c ${process.env.E2E_API_CONFIGURATION ?? "Debug"}`,
        ].join(" && "),
        url: `http://localhost:${apiPort}/health/live`,
        reuseExistingServer: false,
        timeout: 300_000,
        env: {
          ASPNETCORE_ENVIRONMENT: "Development",
          ASPNETCORE_URLS: `http://localhost:${apiPort}`,
          ConnectionStrings__Postgres: database,
          ConnectionStrings__IdentityServerDB: database,
          JwtSettings__PrivateKey: privateKeyBase64,
          Seed__AdminPassword: E2E_TEST_PASSWORD,
          Seed__TestUsersPassword: E2E_TEST_PASSWORD,
          // 10.000 transformadores de demostración: el volumen de RNF-02 (DevelopmentDemoData de la API).
          Seed__DemoTransformers: "10000",
          UseInMemoryDatabase: "false",
          // Los correos quedan en la bandeja de salida: no hace falta un servidor SMTP.
          EmailOutbox__DispatcherEnabled: "false",
          Storage__Provider: "Local",
          Storage__LocalPath: join(tmpdir(), "energymap-e2e-documentos"),
          TrustedProxy__Key: E2E_PROXY_KEY,
          // Los ingresos de todas las pruebas salen de la misma IP: sin esto el límite de /Auth (10 por minuto) las frenaría.
          RateLimiting__AuthPermitLimit: "100000",
          // El seguimiento sí se prueba con un límite bajo: cada prueba manda su propia IP.
          RateLimiting__PublicTrackingPermitLimit: "4",
          App__EmailConfirmationUrl: `http://localhost:${port}/confirmar-correo`,
          App__PasswordResetUrl: `http://localhost:${port}/restablecer`,
        },
      },
    ]
  : [];

export default defineConfig({
  testDir: "./e2e",
  // Con la API, dos servidores de Next y varios navegadores en la misma máquina, la primera carga de una página
  // más el análisis de axe puede pasar de 30 s.
  timeout: 60_000,
  // En local corre `next dev`, que compila cada ruta la primera vez que se visita: con la API y varios navegadores a la
  // vez puede pasar de 5 s. En CI se prueba el build de producción y basta el valor por defecto.
  expect: { timeout: isCI ? 5_000 : 15_000 },
  fullyParallel: true,
  forbidOnly: isCI,
  retries: isCI ? 1 : 0,
  reporter: isCI ? [["list"], ["html", { open: "never" }]] : "list",
  use: {
    baseURL: `http://localhost:${port}`,
    trace: "retain-on-failure",
    locale: "es-DO",
  },
  projects: [
    { name: "escritorio", testIgnore: /\.api\.spec\.ts$/, use: { ...devices["Desktop Chrome"] } },
    // CA-13: funcional desde 360 px de ancho.
    { name: "movil", testIgnore: /\.api\.spec\.ts$/, use: { ...devices["Pixel 7"], viewport: { width: 360, height: 780 } } },
    // Contra la API real: sesión por rol, permisos y perfiles. Cambian datos, así que corren una sola vez y en orden.
    {
      name: "con-api",
      testMatch: /\.api\.spec\.ts$/,
      testIgnore: /(requests|simulations|management)\.api\.spec\.ts$/,
      fullyParallel: false,
      use: { ...devices["Desktop Chrome"] },
    },
    // Solicitudes, simulaciones y gestión (F5 a F7): después de "con-api", que prueba el primer ingreso de los mismos usuarios (perfil sin completar).
    {
      name: "con-api-solicitudes",
      testMatch: /(requests|simulations|management)\.api\.spec\.ts$/,
      dependencies: ["con-api"],
      fullyParallel: false,
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: [
    nextServer(port, true),
    // Solo en CI: el mismo build sin el catálogo, para comprobar que responde 404.
    // En local no se levanta porque dos `next dev` sobre la misma carpeta se bloquean entre sí.
    ...(isCI ? [nextServer(port + 1, false)] : []),
    ...apiServer,
  ],
});
