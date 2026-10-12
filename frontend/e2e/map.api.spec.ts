import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

import { hasApi } from "./support";

// Mapa público contra la API real con los 10.000 transformadores de demostración (RNF-02). Criterios de F3:
// CA-01 a CA-04, lista accesible equivalente y ficha compartible.
test.skip(!hasApi, "Define ENERGYMAP_E2E_DB para ejecutar las pruebas contra la API");

const status = (page: Page) => page.getByRole("status").filter({ hasText: /transformadores|No hay transformadores|Cargando/ });

async function openMap(page: Page, path = "/") {
  await page.goto(path);
  await expect(page.locator("canvas.maplibregl-canvas")).toBeVisible({ timeout: 20_000 });
}

async function searchFor(page: Page, text: string) {
  await page.getByLabel("Buscar transformador por código o coordenadas").fill(text);
  await page.getByRole("button", { name: "Buscar" }).click();
}

test("CA-01: el país completo con los 10.000 transformadores agrupados", async ({ page }) => {
  const visit = async () => {
    const started = Date.now();
    await openMap(page);
    await expect(status(page)).toContainText("10,000 transformadores en el área", { timeout: 30_000 });
    return Date.now() - started;
  };

  // La primera visita incluye la compilación inicial de Next y de la API: se anota, pero no se evalúa.
  const cold = await visit();
  const warm = await visit();
  test.info().annotations.push({ type: "tiempo hasta ver los datos (ms)", description: `primera visita ${cold}, siguiente ${warm}` });

  // CA-01 pide 3 s con 10 Mbps. Aquí corren la API, dos servidores de Next y varios navegadores a la vez:
  // el límite es holgado y sirve para detectar regresiones grandes, no para certificar CA-01.
  expect(warm).toBeLessThan(8_000);
});

test("los grupos del país se calculan rápido en el servidor (RNF-01)", async ({ request }) => {
  const country = "minLatitude=17.4&minLongitude=-72.1&maxLatitude=20&maxLongitude=-68.3&zoom=7";
  await request.get(`/api/mapa/Transformers/clusters?${country}`); // calienta la caché de la API

  const started = Date.now();
  const response = await request.get(`/api/mapa/Transformers/clusters?${country}`);
  const elapsed = Date.now() - started;
  const body = await response.json();

  expect(response.ok()).toBe(true);
  expect(body.data.total).toBe(10_000);
  expect(body.data.items.length).toBeLessThan(500);
  expect(response.headers()["cache-control"]).toContain("public");
  expect(elapsed).toBeLessThan(1_000);
});

test("el proxy del mapa solo deja pasar los endpoints públicos", async ({ request }) => {
  expect((await request.get("/api/mapa/Auth/login")).status()).toBe(404);
  expect((await request.get("/api/mapa/AuditLogs")).status()).toBe(404);
  expect((await request.get("/api/mapa/Transformers/critical-zones")).status()).toBe(404);
  expect((await request.get("/api/mapa/ServiceZones")).ok()).toBe(true);
});

test("CA-04: los filtros refrescan el mapa sin recargar la página y quedan en la URL", async ({ page }) => {
  await openMap(page);
  await expect(status(page)).toContainText("10,000 transformadores", { timeout: 15_000 });
  await page.evaluate(() => ((window as unknown as { sinRecargar: boolean }).sinRecargar = true));

  await page.getByLabel("Estado").selectOption({ label: "Saturada" });
  await expect(page).toHaveURL(/estado=Saturated/);
  await expect(status(page)).not.toContainText("10,000");
  const saturated = Number((await status(page).textContent())!.replace(/\D+/g, " ").trim().split(" ")[0]);
  expect(saturated).toBeGreaterThan(100);
  expect(saturated).toBeLessThan(10_000);

  await page.getByLabel("Zona").selectOption({ label: "Barahona (Barahona)" });
  await expect(page).toHaveURL(/zona=\d+/);
  await expect(status(page)).not.toContainText(`${saturated.toLocaleString("es-DO")} transformadores`);

  await page.getByRole("button", { name: "Quitar filtros" }).click();
  await expect(status(page)).toContainText("10,000 transformadores");
  expect(await page.evaluate(() => (window as unknown as { sinRecargar?: boolean }).sinRecargar)).toBe(true);
});

test("RF-04 y RF-05: buscar por código abre el detalle técnico", async ({ page }) => {
  await openMap(page);
  await searchFor(page, "TR-DEMO-00001");

  const detail = page.getByRole("region", { name: "Transformador seleccionado" });
  // Margen amplio: puede ser el primer detalle que pide la API recién arrancada (EF compila la consulta, ~4 s).
  await expect(detail.getByRole("heading", { name: "TR-DEMO-00001" })).toBeVisible({ timeout: 15_000 });
  await expect(detail.getByText("Capacidad total")).toBeVisible();
  await expect(detail.getByText(/Actualizado el/)).toBeVisible();
  await expect(detail.getByRole("meter").or(detail.getByText("no se calcula durante el mantenimiento"))).toBeVisible();
  await expect(page).toHaveURL(/[?&]t=\d+/);

  await detail.getByRole("link", { name: "Ver la ficha completa" }).click();
  await expect(page.getByRole("heading", { level: 1, name: "TR-DEMO-00001" })).toBeVisible();
});

test("buscar por coordenadas lista los cercanos y al acercarse aparece la lista del área", async ({ page }) => {
  await openMap(page);
  await searchFor(page, "18.4861, -69.9312");

  const results = page.getByRole("button", { name: /TR-DEMO-/ });
  await expect(results.first()).toBeVisible();
  await results.first().click();
  await expect(page.getByRole("region", { name: "Transformador seleccionado" })).toBeVisible();

  // Cerrar el detalle muestra la lista del área visible (el mapa se acercó al transformador).
  await page.getByRole("button", { name: "Cerrar" }).click();
  const list = page.getByRole("region", { name: "En el área" });
  await expect(list.getByRole("button").first()).toBeVisible({ timeout: 15_000 });
  await expect(status(page)).toContainText(/\d+ transformadores en el área\./);

  // Teclado: la lista es la alternativa al mapa.
  await list.getByRole("button").first().focus();
  await page.keyboard.press("Enter");
  await expect(page.getByRole("region", { name: "Transformador seleccionado" })).toBeVisible();
});

test("un enlace con ?t= abre el mapa con ese transformador", async ({ page, request }) => {
  const search = await (await request.get("/api/mapa/Transformers/search?code=TR-DEMO-00042")).json();
  const id = search.data[0].id as number;

  await openMap(page, `/?t=${id}`);
  await expect(page.getByRole("region", { name: "Transformador seleccionado" }).getByRole("heading", { name: "TR-DEMO-00042" })).toBeVisible();
});

test("lista accesible: los mismos datos y filtros sin el mapa", async ({ page }) => {
  await page.goto("/transformadores");
  await expect(page.getByRole("heading", { level: 1, name: "Transformadores" })).toBeVisible();
  await expect(page.getByRole("status")).toContainText("se muestran los primeros 5,000");

  await page.getByLabel("Zona").selectOption({ label: "Barahona (Barahona)" });
  await page.getByLabel("Estado").selectOption({ label: "Crítica" });
  await page.getByRole("button", { name: "Filtrar" }).click();

  await expect(page).toHaveURL(/estado=Critical/);
  const rows = page.getByRole("region", { name: "Transformadores" }).getByRole("row");
  await expect(rows.nth(1)).toBeVisible();
  const badges = page.getByRole("region", { name: "Transformadores" }).locator("[data-status]");
  const statuses = await badges.evaluateAll((els) => els.map((e) => e.getAttribute("data-status")));
  expect(new Set(statuses)).toEqual(new Set(["Critical"]));

  await page.getByRole("region", { name: "Transformadores" }).getByRole("link").first().click();
  await expect(page.getByRole("heading", { level: 1, name: /TR-DEMO-/ })).toBeVisible();
});

test("la lista se pagina", async ({ page }) => {
  await page.goto("/transformadores?estado=Available");
  await expect(page.getByRole("status")).toContainText("Página 1 de");
  await page.getByRole("link", { name: "Siguiente" }).click();
  await expect(page).toHaveURL(/pagina=2/);
  await expect(page.getByRole("status")).toContainText("Página 2 de");
});

test("una ficha que no existe responde 404", async ({ page }) => {
  expect((await page.goto("/transformadores/999999999"))?.status()).toBe(404);
  expect((await page.goto("/transformadores/abc"))?.status()).toBe(404);
});

test("mapa con detalle abierto y lista sin violaciones de accesibilidad graves", async ({ page }) => {
  for (const colorScheme of ["light", "dark"] as const) {
    await page.emulateMedia({ colorScheme });
    await openMap(page);
    await searchFor(page, "TR-DEMO-00007");
    await expect(page.getByRole("region", { name: "Transformador seleccionado" }).getByRole("heading", { name: "TR-DEMO-00007" })).toBeVisible();

    for (const target of [page, null]) {
      if (!target) await page.goto("/transformadores?zona=1");
      const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
      const serious = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");
      expect(serious.map((v) => `${colorScheme} ${page.url()} ${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`)).toEqual([]);
    }
  }
});
