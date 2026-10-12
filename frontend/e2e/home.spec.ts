import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

test.describe("página inicial (F0)", () => {
  test("dibuja el mapa base sin descargar código de terceros", async ({ page }) => {
    const thirdPartyScripts: string[] = [];
    const consoleErrors: string[] = [];

    page.on("request", (request) => {
      if (request.url().includes("unpkg.com")) thirdPartyScripts.push(request.url());
    });
    page.on("console", (message) => {
      // Sin API (ENERGYMAP_E2E_DB vacía) el mapa no tiene datos y el navegador registra el 503 del recurso:
      // eso no es un error de la página.
      if (message.type() === "error" && !message.text().startsWith("Failed to load resource")) consoleErrors.push(message.text());
    });

    await page.goto("/");

    await expect(page.getByRole("link", { name: "EnergyMap" })).toBeVisible();
    await expect(page.getByRole("heading", { level: 1, name: "Mapa de disponibilidad eléctrica" })).toBeAttached();
    await expect(page.getByRole("region", { name: "Mapa de República Dominicana" })).toBeVisible();
    // El lienzo solo aparece si MapLibre arrancó, y para eso necesita el worker servido desde nuestro origen.
    await expect(page.locator("canvas.maplibregl-canvas")).toBeVisible({ timeout: 20_000 });
    // El lienzo puede existir dentro de un contenedor de altura 0 y no verse: se mide el contenedor.
    const mapBox = await page.locator(".maplibregl-map").boundingBox();
    expect(mapBox?.height ?? 0).toBeGreaterThan(300);
    await expect(page.getByRole("button", { name: "Acercar" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Alejar" })).toBeVisible();

    expect(thirdPartyScripts).toEqual([]);
    expect(consoleErrors).toEqual([]);
  });

  test("sirve el worker de MapLibre desde el propio sitio", async ({ request }) => {
    const pkg = (await import("maplibre-gl/package.json", { with: { type: "json" } })).default;
    const response = await request.get(`/vendor/maplibre-gl/${pkg.version}/maplibre-gl-worker.mjs`);

    expect(response.status()).toBe(200);
    expect(response.headers()["content-type"]).toContain("javascript");
  });

  test("no tiene violaciones de accesibilidad graves (WCAG 2.1 AA)", async ({ page }) => {
    await page.goto("/");
    await expect(page.locator("canvas.maplibregl-canvas")).toBeVisible({ timeout: 20_000 });

    const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
    const serious = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");

    expect(serious.map((v) => `${v.id}: ${v.help} (${v.nodes.length})`)).toEqual([]);
  });
});
