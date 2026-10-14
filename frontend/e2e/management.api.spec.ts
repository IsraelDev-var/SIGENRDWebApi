import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

import { API_URL, apiDataAs, createSubmittedAsCustomer, ensureCustomerProfile, findOpenTransformer, formAlert, hasApi, login, type OpenTransformer } from "./support";

// F7: gestión (bandeja, revisión, transformadores e importación) contra la API real.
test.skip(!hasApi, "Define ENERGYMAP_E2E_DB para ejecutar las pruebas contra la API");

let transformer: OpenTransformer;

test.beforeAll(async ({ request }) => {
  await ensureCustomerProfile(request);
  transformer = await findOpenTransformer(request, 2);
});

async function openReview(page: Page, code: string) {
  await page.goto(`/gestion/solicitudes?texto=${code}`);
  await page.getByRole("link", { name: code }).click();
  await expect(page.getByRole("heading", { level: 1, name: code })).toBeVisible();
}

test("el ingeniero pasa a revisión y observa, pero no aprueba ni rechaza", async ({ page, request }) => {
  const { code } = await createSubmittedAsCustomer(request, transformer, "Calle de la bandeja 1");
  await login(page, "Engineer");

  // La bandeja la muestra con su titular.
  await page.getByRole("link", { name: "Solicitudes" }).first().click();
  await page.getByLabel("Expediente o dirección").fill(code);
  await page.getByRole("button", { name: "Filtrar" }).click();
  await expect(page.getByRole("region", { name: "Bandeja de solicitudes" }).getByText("Cliente de prueba")).toBeVisible();

  await openReview(page, code);
  await expect(page.getByRole("region", { name: "Datos de la solicitud" }).getByText("Calle de la bandeja 1")).toBeVisible();
  await page.getByRole("button", { name: "Pasar a revisión" }).click();
  await expect(page.getByRole("status").filter({ hasText: "pasó a revisión" })).toBeVisible();
  await expect(page.locator("[data-status=UnderReview]").first()).toBeVisible();

  // Matriz 3.2: el ingeniero nunca ve aprobar ni rechazar.
  await expect(page.getByRole("button", { name: "Aprobar" })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Rechazar" })).toHaveCount(0);
  await expect(page.getByText("La aprobación o el rechazo los decide la distribuidora.")).toBeVisible();

  // RN-13: las observaciones llevan comentario.
  await page.getByRole("button", { name: "Emitir observaciones" }).click();
  await page.getByRole("button", { name: "Enviar observaciones" }).click();
  await expect(page.getByText("Escribe qué debe corregir el solicitante (RN-13).").first()).toBeVisible();
  await page.getByLabel("Tipo de observación").selectOption("Documental");
  await page.getByLabel("Qué debe corregir el solicitante").fill("Falta la cédula legible.");
  await page.getByRole("button", { name: "Enviar observaciones" }).click();
  await expect(page.locator("[data-status=Observed]").first()).toBeVisible();
  await expect(page.getByRole("region", { name: "Historial" }).getByText("Falta la cédula legible.")).toBeVisible();
});

test("la distribuidora aprueba y descarga el certificado; rechazar exige motivo", async ({ page, request }) => {
  const approved = await createSubmittedAsCustomer(request, transformer, "Calle aprobada por gestión 2");
  const rejected = await createSubmittedAsCustomer(request, transformer, "Calle rechazada por gestión 3");
  await login(page, "Distributor");

  await openReview(page, approved.code);
  await page.getByRole("button", { name: "Pasar a revisión" }).click();
  await page.getByRole("button", { name: "Aprobar" }).click();
  await page.getByRole("button", { name: "Sí, aprobar" }).click();
  await expect(page.locator("[data-status=Approved]").first()).toBeVisible();

  const downloadPromise = page.waitForEvent("download");
  await page.getByRole("link", { name: "Descargar el certificado" }).click();
  const chunks: Buffer[] = [];
  for await (const chunk of await (await downloadPromise).createReadStream()) chunks.push(chunk as Buffer);
  expect(Buffer.concat(chunks).subarray(0, 5).toString()).toBe("%PDF-");

  await openReview(page, rejected.code);
  await page.getByRole("button", { name: "Pasar a revisión" }).click();
  await page.getByRole("button", { name: "Rechazar" }).click();
  await page.getByRole("button", { name: "Rechazar la solicitud" }).click();
  await expect(page.getByText("Escribe el motivo del rechazo (RN-13).").first()).toBeVisible();
  await page.getByLabel("Motivo del rechazo").fill("El transformador no cubre la carga del proyecto.");
  await page.getByRole("button", { name: "Rechazar la solicitud" }).click();
  await expect(page.locator("[data-status=Rejected]").first()).toBeVisible();
});

test("CA-07: una transición que ya no vale muestra el motivo de la API", async ({ page, request }) => {
  const { id, code } = await createSubmittedAsCustomer(request, transformer, "Calle concurrente 4");
  await apiDataAs(request, "Distributor", `/ConnectionRequest/${id}/start-review`, { method: "POST" });

  await login(page, "Distributor");
  await openReview(page, code);
  await page.getByRole("button", { name: "Aprobar" }).click();

  // Mientras tanto, otra persona la rechaza.
  await apiDataAs(request, "Admin", `/ConnectionRequest/${id}/reject`, { method: "POST", data: { comment: "Rechazada por otra persona." } });

  await page.getByRole("button", { name: "Sí, aprobar" }).click();
  await expect(formAlert(page, "Recarga la página para ver el estado actual.")).toBeVisible();
  await page.reload();
  await expect(page.locator("[data-status=Rejected]").first()).toBeVisible();
});

test("el ingeniero no gestiona la red: ni menú ni acceso por URL", async ({ page }) => {
  await login(page, "Engineer");
  await expect(page.getByRole("link", { name: "Transformadores" })).toHaveCount(0);
  await page.goto("/gestion/transformadores");
  await expect(page).toHaveURL("/sin-acceso");
});

test("la distribuidora actualiza capacidad y mantenimiento, y el estado lo recalcula la API", async ({ page, request }) => {
  const created = `TR-E2E-${Date.now().toString().slice(-7)}`;
  await login(page, "Distributor");

  await page.goto("/gestion/transformadores/nuevo");
  await page.getByLabel("Código").fill(created);
  await page.getByLabel("Capacidad total (kVA)").fill("100");
  await page.getByLabel("Capacidad disponible (kVA)").fill("120");
  await page.getByLabel("Latitud").fill("18.47");
  await page.getByLabel("Longitud").fill("-69.89");
  await page.getByRole("button", { name: "Crear transformador" }).click();
  await expect(page.getByText("La disponible no puede superar la total.")).toBeVisible();
  await page.getByLabel("Capacidad disponible (kVA)").fill("90");
  await page.getByRole("button", { name: "Crear transformador" }).click();

  await expect(page).toHaveURL(/\/gestion\/transformadores\/\d+\?creado=1$/);
  await expect(page.getByRole("heading", { level: 1, name: created })).toBeVisible();
  await expect(page.locator("[data-status=Available]").first()).toBeVisible();

  // 10 de 100 libres: la API lo pasa a Crítica (RN-02).
  await page.getByLabel("Capacidad disponible (kVA)").fill("10");
  await page.getByRole("button", { name: "Guardar capacidad" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Capacidad actualizada" })).toBeVisible();
  await expect(page.locator("[data-status=Critical]").first()).toBeVisible();

  await page.getByRole("button", { name: "Poner en mantenimiento" }).click();
  await expect(page.locator("[data-status=Maintenance]").first()).toBeVisible();
  const api = await (await request.get(`${API_URL}/Transformers/search?code=${created}`)).json();
  expect(api.data[0].status).toBe("Maintenance");

  // En la lista de gestión se encuentra por código.
  await page.goto(`/gestion/transformadores?codigo=${created}`);
  await expect(page.getByRole("region", { name: "Red de transformadores" }).getByRole("link", { name: created })).toBeVisible();
});

test("importación CSV: los errores se muestran por línea y no se importa nada", async ({ page, request }) => {
  const zones = (await (await request.get(`${API_URL}/ServiceZones`)).json()).data as { id: number }[];
  const zone = zones[0]!.id;
  const stamp = Date.now().toString().slice(-7);
  const bad = [
    "code,serviceZoneId,totalCapacityKva,availableCapacityKva,latitude,longitude",
    `TR-CSV-${stamp}-1,${zone},100,50,18.5,-69.9`,
    `TR-CSV-${stamp}-2,999999,100,50,18.5,-69.9`,
    `TR-DEMO-00001,${zone},100,50,18.5,-69.9`,
  ].join("\n");

  await login(page, "Distributor");
  await page.goto("/gestion/transformadores/importar");
  await page.getByLabel("Archivo CSV (máximo 5 MB)").setInputFiles({ name: "red.csv", mimeType: "text/csv", buffer: Buffer.from(bad) });
  await page.getByRole("button", { name: "Importar" }).click();

  await expect(formAlert(page, "No se importó nada")).toBeVisible();
  const errors = page.getByRole("region", { name: "Errores de la importación" });
  await expect(errors.getByRole("row").filter({ hasText: "la zona 999999 no existe" }).getByRole("cell").first()).toHaveText("3");
  await expect(errors.getByRole("row").filter({ hasText: "TR-DEMO-00001" }).getByRole("cell").first()).toHaveText("4");
  const none = await (await request.get(`${API_URL}/Transformers/search?code=TR-CSV-${stamp}`)).json();
  expect(none.data).toEqual([]);

  const good = [
    "code,serviceZoneId,totalCapacityKva,availableCapacityKva,latitude,longitude",
    `TR-CSV-${stamp}-1,${zone},100,50,18.5,-69.9`,
    `TR-CSV-${stamp}-2,${zone},200,180,18.51,-69.91`,
  ].join("\n");
  await page.getByLabel("Archivo CSV (máximo 5 MB)").setInputFiles({ name: "red.csv", mimeType: "text/csv", buffer: Buffer.from(good) });
  await page.getByRole("button", { name: "Importar" }).click();
  await expect(page.getByRole("status").filter({ hasText: "2 transformadores importados." })).toBeVisible();
});

test("gestión sin violaciones de accesibilidad graves, también a 360 px", async ({ page, request }) => {
  test.setTimeout(240_000); // 7 páginas × 2 anchos × 2 temas con axe.
  const { id } = await createSubmittedAsCustomer(request, transformer, "Calle accesible de gestión 5");
  await login(page, "Distributor");

  for (const viewport of [
    { width: 1280, height: 800 },
    { width: 360, height: 780 },
  ]) {
    await page.setViewportSize(viewport);
    for (const colorScheme of ["light", "dark"] as const) {
      await page.emulateMedia({ colorScheme });
      for (const path of [
        "/gestion",
        "/gestion/solicitudes",
        `/gestion/solicitudes/${id}`,
        "/gestion/transformadores",
        `/gestion/transformadores/${transformer.id}`,
        "/gestion/transformadores/importar",
        "/gestion/instaladoras?estado=",
      ]) {
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
