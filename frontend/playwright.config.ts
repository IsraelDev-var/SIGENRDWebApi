import { defineConfig, devices } from "@playwright/test";

const port = Number(process.env.PORT ?? 3000);
const isCI = !!process.env.CI;

export default defineConfig({
  testDir: "./e2e",
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
    { name: "escritorio", use: { ...devices["Desktop Chrome"] } },
    // CA-13: funcional desde 360 px de ancho.
    { name: "movil", use: { ...devices["Pixel 7"], viewport: { width: 360, height: 780 } } },
  ],
  // En CI se prueba el build de producción; en local, el servidor de desarrollo.
  webServer: {
    command: isCI ? `pnpm start --port ${port}` : `pnpm dev --port ${port}`,
    url: `http://localhost:${port}`,
    reuseExistingServer: !isCI,
    timeout: 120_000,
    // Activa el catálogo del Design System, que responde 404 sin esta variable.
    env: { ENERGYMAP_DESIGN_CATALOG: "true" },
  },
});
