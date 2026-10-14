import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

import { apiAs, apiDataAs, createSimulationAsCustomer, ensureCustomerProfile, findOpenTransformer, hasApi, login } from "./support";

// F6: simulador de medición neta contra la API real. El frontend no calcula: se comprueba que muestra los valores de la API.
test.skip(!hasApi, "Define ENERGYMAP_E2E_DB para ejecutar las pruebas contra la API");

type Simulation = {
  id: number;
  annualSavings: number;
  paybackYears: number | null;
  roi: number;
  coveragePct: number;
  installedCapacityKw: number;
  viabilityStatus: string;
  disclaimer: string;
  referenceTransformer?: { id: number; code: string } | null;
};

const money = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP", maximumFractionDigits: 0 });
const oneDecimal = new Intl.NumberFormat("es-DO", { minimumFractionDigits: 1, maximumFractionDigits: 1 });

// El paso a la solicitud necesita el perfil del cliente (lo crea session.api.spec.ts; aquí por si se ejecuta solo).
test.beforeAll(async ({ request }) => {
  await ensureCustomerProfile(request);
});

function idFrom(page: Page) {
  return Number(new URL(page.url()).pathname.split("/").pop());
}

test("RF-18 a RF-21: el resultado muestra exactamente lo que calculó la API, con el aviso informativo", async ({ page, request }) => {
  await login(page, "Customer");
  await page.goto("/portal/simulaciones/nueva");

  await page.getByRole("button", { name: "Calcular" }).click();
  await expect(page.getByText("Escribe tu consumo mensual en kWh, por ejemplo 450.")).toBeVisible();
  await expect(page.getByText("Escribe la potencia estimada en kWp, por ejemplo 3.5.")).toBeVisible();

  await page.getByLabel("Consumo mensual (kWh)").fill("650");
  await page.getByLabel("Potencia estimada (kWp)").fill("4");
  await page.getByLabel("Nombre para recordarla (opcional)").fill("Casa de prueba");
  await page.getByRole("button", { name: "Calcular" }).click();

  // La primera simulación con la API recién arrancada compila sus consultas (búsqueda de cercanos entre 10.000).
  await expect(page).toHaveURL(/\/portal\/simulaciones\/\d+$/, { timeout: 45_000 });
  const api = await apiDataAs<Simulation>(request, "Customer", `/Simulations/${idFrom(page)}`);

  await expect(page.getByRole("heading", { level: 1, name: "Casa de prueba" })).toBeVisible();
  await expect(page.getByRole("note")).toHaveText(api.disclaimer);
  await expect(page.locator(`[data-viability=${api.viabilityStatus}]`).first()).toBeVisible();

  const summary = page.getByRole("region", { name: "Resumen" });
  await expect(summary.getByText(money.format(api.annualSavings), { exact: true })).toBeVisible();
  await expect(summary.getByText(api.paybackYears == null ? "Sin retorno" : `${oneDecimal.format(api.paybackYears)} años`, { exact: true })).toBeVisible();
  await expect(summary.getByText(`${oneDecimal.format(api.roi)} %`, { exact: true })).toBeVisible();
  await expect(summary.getByText(`${oneDecimal.format(api.coveragePct)} %`, { exact: true })).toBeVisible();

  // Queda en el historial (RF-26).
  await page.getByRole("link", { name: "Simulaciones", exact: true }).first().click();
  await expect(page.getByRole("region", { name: "Mis simulaciones" }).getByRole("link", { name: "Casa de prueba" }).first()).toBeVisible();
});

test("RF-24: con los paneles registrados, la potencia es la suma de los paneles", async ({ page, request }) => {
  await login(page, "Customer", "/ingresar?next=/portal/simulaciones/nueva");
  await page.getByLabel("Consumo mensual (kWh)").fill("400");
  await page.getByLabel("Tengo los equipos").check();
  await page.getByLabel("Potencia de cada panel (kW)").fill("0.55");
  await page.getByLabel("Cantidad").fill("8");
  await page.getByRole("button", { name: "Agregar equipo" }).click();
  await page.getByLabel("Tipo").nth(1).selectOption("inverter");
  await page.getByLabel("Modelo del inversor").fill("Inversor 4 kW");
  await page.getByLabel("Potencia del inversor (kW)").fill("4");
  await page.getByRole("button", { name: "Calcular" }).click();

  await expect(page).toHaveURL(/\/portal\/simulaciones\/\d+$/);
  const api = await apiDataAs<Simulation>(request, "Customer", `/Simulations/${idFrom(page)}`);
  expect(api.installedCapacityKw).toBe(4.4);
  await expect(page.getByRole("region", { name: "Detalle del cálculo" }).getByText("4.4 kWp")).toBeVisible();
  await expect(page.getByRole("region", { name: "Equipos" }).getByText("Inversor Inversor 4 kW")).toBeVisible();
});

test("CA-10: el resumen se descarga en PDF", async ({ page, request }) => {
  const { id } = await createSimulationAsCustomer(request, { name: "Para el PDF", monthlyConsumptionKwh: 500, installedCapacityKw: 3 });
  await login(page, "Customer", `/ingresar?next=/portal/simulaciones/${id}`);

  const downloadPromise = page.waitForEvent("download");
  await page.getByRole("link", { name: "Descargar en PDF" }).click();
  const download = await downloadPromise;
  expect(download.suggestedFilename()).toBe(`Simulacion-${id}.pdf`);
  const chunks: Buffer[] = [];
  for await (const chunk of await download.createReadStream()) chunks.push(chunk as Buffer);
  expect(Buffer.concat(chunks).subarray(0, 5).toString()).toBe("%PDF-");
});

test("RF-22: compara dos simulaciones y marca la de mayor ahorro", async ({ page, request }) => {
  const small = await createSimulationAsCustomer(request, { name: "Comparar pequeña", monthlyConsumptionKwh: 300, installedCapacityKw: 1.5 });
  const big = await createSimulationAsCustomer(request, { name: "Comparar grande", monthlyConsumptionKwh: 900, installedCapacityKw: 6 });
  const comparison = await apiDataAs<{ highestSavingsId: number; simulations: Simulation[] }>(
    request,
    "Customer",
    `/Simulations/compare?ids=${small.id}&ids=${big.id}`,
  );
  const winner = comparison.simulations.find((sim) => sim.id === comparison.highestSavingsId)!;

  await login(page, "Customer", "/ingresar?next=/portal/simulaciones");
  // Una sola: avisa en vez de fallar.
  await page.getByLabel("Comparar Comparar pequeña").check();
  await page.getByRole("button", { name: "Comparar las marcadas" }).click();
  await expect(page.getByText("Elige de 2 a 5 simulaciones")).toBeVisible();

  await page.goBack();
  await page.getByLabel("Comparar Comparar pequeña").check();
  await page.getByLabel("Comparar Comparar grande").check();
  await page.getByRole("button", { name: "Comparar las marcadas" }).click();

  const table = page.getByRole("region", { name: "Comparación de simulaciones" });
  await expect(table.getByRole("columnheader", { name: "Comparar pequeña" })).toBeVisible();
  await expect(table.getByRole("columnheader", { name: "Comparar grande" })).toBeVisible();
  // La marca la decide la API y queda en la columna de esa simulación (el orden de columnas sigue al del historial).
  const savingsRow = table.getByRole("row").filter({ hasText: "Ahorro anual" });
  await expect(savingsRow.getByRole("cell").filter({ hasText: "Mayor ahorro" })).toHaveCount(1);
  await expect(savingsRow.getByRole("cell").filter({ hasText: "Mayor ahorro" })).toContainText(money.format(winner.annualSavings));
});

test("del resultado a una solicitud de medición neta con los datos de la simulación", async ({ page, request }) => {
  // En el punto de un transformador disponible: ese es el más cercano y el de referencia.
  const open = await findOpenTransformer(request);
  const { id } = await createSimulationAsCustomer(request, {
    name: "Hacia la solicitud",
    monthlyConsumptionKwh: 700,
    installedCapacityKw: 5,
    latitude: open.latitude,
    longitude: open.longitude,
  });
  const api = await apiDataAs<Simulation>(request, "Customer", `/Simulations/${id}`);
  expect(api.referenceTransformer?.id).toBe(open.id);

  await login(page, "Customer", `/ingresar?next=/portal/simulaciones/${id}`);
  await page.getByRole("link", { name: "Solicitar medición neta con estos datos" }).click();

  await expect(page).toHaveURL(`/portal/solicitudes/nueva?transformador=${api.referenceTransformer!.id}&simulacion=${id}`);
  await expect(page.getByText("Tomamos de la simulación «Hacia la solicitud»")).toBeVisible();
  await expect(page.getByLabel("Medición neta")).toBeChecked();
});

test("desde la ficha del transformador se simula con su ubicación", async ({ page, request }) => {
  const transformer = await findOpenTransformer(request);
  await login(page, "Customer", `/ingresar?next=/transformadores/${transformer.id}`);
  await page.getByRole("link", { name: /Simular paneles solares/ }).click();
  await expect(page).toHaveURL(/\/portal\/simulaciones\/nueva\?lat=/);
  await expect(page.getByLabel("Latitud")).toHaveValue(transformer.latitude.toFixed(6));
});

test("RN-22: otro usuario no ve la simulación ni descarga su PDF", async ({ page, request }) => {
  const { id } = await createSimulationAsCustomer(request, { name: "Privada", monthlyConsumptionKwh: 350, installedCapacityKw: 2 });
  expect((await apiAs(request, "InstallerCompany", `/Simulations/${id}`)).status()).toBe(404);

  await login(page, "InstallerCompany", `/ingresar?next=/portal/simulaciones/${id}`);
  await expect(page.getByRole("heading", { name: "No encontramos esa simulación" })).toBeVisible();
  expect((await page.request.get(`/portal/simulaciones/${id}/reporte`)).status()).toBe(404);
});

test("simulaciones sin violaciones de accesibilidad graves, también a 360 px", async ({ page, request }) => {
  const a = await createSimulationAsCustomer(request, { name: "Axe A", monthlyConsumptionKwh: 450, installedCapacityKw: 3 });
  const b = await createSimulationAsCustomer(request, { name: "Axe B", monthlyConsumptionKwh: 800, installedCapacityKw: 5 });
  await login(page, "Customer");

  for (const viewport of [
    { width: 1280, height: 800 },
    { width: 360, height: 780 },
  ]) {
    await page.setViewportSize(viewport);
    for (const colorScheme of ["light", "dark"] as const) {
      await page.emulateMedia({ colorScheme });
      for (const path of ["/portal/simulaciones", "/portal/simulaciones/nueva", `/portal/simulaciones/${a.id}`, `/portal/simulaciones/comparar?ids=${a.id}&ids=${b.id}`]) {
        await page.goto(path);
        await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
        const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
        const serious = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");
        expect(serious.map((v) => `${viewport.width} ${colorScheme} ${path} ${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`)).toEqual([]);
        const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
        expect(overflow, `${viewport.width} ${path} se desplaza en horizontal`).toBe(false);
      }
    }
  }
});
