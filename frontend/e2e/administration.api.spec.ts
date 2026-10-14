import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

import {
  API_URL,
  apiAs,
  apiDataAs,
  createSubmittedAsCustomer,
  ensureCustomerProfile,
  ensureValidatedInstaller,
  findOpenTransformer,
  formAlert,
  hasApi,
  login,
  logout,
  uploadRequiredDocuments,
} from "./support";

// F8: reportes, zonas críticas, exportación, usuarios internos y auditoría contra la API real.
test.skip(!hasApi, "Define ENERGYMAP_E2E_DB para ejecutar las pruebas contra la API");

test.beforeAll(async ({ request }) => {
  await ensureCustomerProfile(request);
  await ensureValidatedInstaller(request);
  const transformer = await findOpenTransformer(request, 4);

  // Datos para los reportes: una solicitud del cliente y una de la instaladora (para el ranking, RF-28), enviadas.
  await createSubmittedAsCustomer(request, transformer, "Calle de los reportes 1");
  const installerRequest = await apiDataAs<number>(request, "InstallerCompany", "/ConnectionRequest", {
    method: "POST",
    data: {
      distributorId: transformer.distributorId,
      transformerId: transformer.id,
      requestType: "Connection",
      requestedCapacityKva: 8,
      usageType: "Residential",
      tariffType: "BT",
      interconnectionType: "Parallel",
      projectAddress: "Calle de la instaladora en reportes 2",
      latitude: transformer.latitude,
      longitude: transformer.longitude,
      propertyRelation: "Owner",
      declarationAccepted: true,
      holder: { fullName: "Titular de reportes", nationalIdOrRnc: `402${Date.now().toString().slice(-8)}`, isLegalEntity: false },
    },
  });
  await uploadRequiredDocuments(request, "InstallerCompany", installerRequest);
  await apiDataAs(request, "InstallerCompany", `/ConnectionRequest/${installerRequest}/submit`, { method: "POST" });
});

test("RF-27 y RF-28: los indicadores son los de la API y el ranking muestra a la instaladora", async ({ page, request }) => {
  await login(page, "Distributor");
  await page.getByRole("link", { name: "Reportes" }).first().click();
  await expect(page.getByRole("heading", { level: 1, name: "Reportes" })).toBeVisible();

  const from = await page.getByLabel("Desde").inputValue();
  const to = await page.getByLabel("Hasta").inputValue();
  const next = new Date(Date.parse(to) + 86_400_000).toISOString().slice(0, 10);
  const stats = await apiDataAs<{ total: number; approved: number; rejected: number }>(
    request,
    "Distributor",
    `/Reports/requests/statistics?From=${from}&To=${next}`,
  );
  expect(stats.total).toBeGreaterThan(0);

  const summary = page.getByRole("region", { name: "Resumen" });
  await expect(summary.getByText(String(stats.total), { exact: true })).toBeVisible();
  await expect(summary.getByText(`${stats.approved} aprobadas · ${stats.rejected} rechazadas`)).toBeVisible();

  // El gráfico tiene su versión en tabla (accesible).
  await expect(page.getByRole("img", { name: /Gráfico de barras con las solicitudes de cada mes/ })).toBeVisible();
  await expect(page.getByRole("region", { name: "Ranking de instaladoras" }).getByText("Solar Caribe SRL")).toBeVisible();

  // Un período al revés no llama a la API: lo explica.
  await page.getByLabel("Desde").fill("2026-12-01");
  await page.getByLabel("Hasta").fill("2026-01-01");
  await page.getByRole("button", { name: "Ver" }).click();
  await expect(page.getByText("La fecha inicial debe ser anterior a la final.")).toBeVisible();
});

test("RF-30: el reporte se exporta en Excel y en PDF", async ({ page }) => {
  await login(page, "Distributor", "/ingresar?next=/gestion/reportes");

  for (const [name, signature] of [
    ["Excel", "PK"],
    ["PDF", "%PDF-"],
  ] as const) {
    const downloadPromise = page.waitForEvent("download");
    await page.getByRole("link", { name }).click();
    const download = await downloadPromise;
    const chunks: Buffer[] = [];
    for await (const chunk of await download.createReadStream()) chunks.push(chunk as Buffer);
    expect(Buffer.concat(chunks).subarray(0, signature.length).toString(), `${name}: ${download.suggestedFilename()}`).toBe(signature);
  }
});

test("RF-29: las zonas críticas son las de la API y llevan a sus transformadores", async ({ page, request }) => {
  const zones = await apiDataAs<{ serviceZoneId: number; zoneName: string }[]>(request, "Distributor", "/Transformers/critical-zones");
  expect(zones.length).toBeGreaterThan(0);

  await login(page, "Distributor");
  await page.getByRole("link", { name: "Zonas críticas" }).first().click();
  const table = page.getByRole("region", { name: "Zonas críticas" });
  await expect(table.getByRole("row")).toHaveCount(zones.length + 1);

  const first = zones[0]!;
  await table.getByRole("link", { name: `Lista de ${first.zoneName}` }).click();
  await expect(page).toHaveURL(new RegExp(`/gestion/transformadores\\?zona=${first.serviceZoneId}`));
  await expect(page.getByLabel("Zona")).toHaveValue(String(first.serviceZoneId));
});

test("matriz 3.2: usuarios y auditoría solo para el administrador; reportes no para el ingeniero", async ({ page }) => {
  await login(page, "Distributor");
  const menu = page.getByRole("navigation", { name: "Gestión" });
  await expect(menu.getByRole("link", { name: "Reportes" })).toBeVisible();
  await expect(menu.getByRole("link", { name: "Usuarios" })).toHaveCount(0);
  await expect(menu.getByRole("link", { name: "Auditoría" })).toHaveCount(0);
  for (const path of ["/gestion/usuarios", "/gestion/auditoria"]) {
    await page.goto(path);
    await expect(page).toHaveURL("/sin-acceso");
  }
  await page.goto("/gestion");
  await logout(page);

  await login(page, "Engineer");
  await expect(page.getByRole("navigation", { name: "Gestión" }).getByRole("link", { name: "Reportes" })).toHaveCount(0);
  await page.goto("/gestion/reportes");
  await expect(page).toHaveURL("/sin-acceso");
  expect((await page.request.get("/gestion/reportes/exportar?formato=Pdf")).status()).toBe(403);
});

test("el administrador crea un usuario interno y lo desactiva y reactiva", async ({ page, request }) => {
  const email = `ingeniero-${Date.now()}@test.energymap.local`;
  await login(page, "Admin");
  await page.getByRole("link", { name: "Usuarios" }).first().click();

  await page.getByText("Nuevo usuario interno").click();
  await page.getByLabel("Nombre completo").fill("Ingeniera de Prueba");
  await page.getByLabel("Correo", { exact: true }).fill(email);
  await page.getByLabel("Contraseña temporal").fill("Temporal-2026!");
  await page.getByLabel("Repite la contraseña").fill("Otra-2026!");
  await page.getByRole("button", { name: "Crear usuario" }).click();
  await expect(page.getByText("Las contraseñas no coinciden.")).toBeVisible();
  await expect(page.getByLabel("Correo", { exact: true })).toHaveValue(email);

  await page.getByLabel("Contraseña temporal").fill("Temporal-2026!");
  await page.getByLabel("Repite la contraseña").fill("Temporal-2026!");
  await page.getByRole("button", { name: "Crear usuario" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Cuenta creada" })).toBeVisible();

  await page.getByLabel("Nombre o correo").fill(email);
  await page.getByRole("button", { name: "Buscar" }).click();
  const row = page.getByRole("region", { name: "Usuarios internos" }).getByRole("row").filter({ hasText: email });
  await expect(row.getByRole("cell", { name: "Ingeniero", exact: true })).toBeVisible();
  await expect(row.getByText("Sin confirmar el correo")).toBeVisible();

  await row.getByRole("button", { name: "Desactivar a Ingeniera de Prueba" }).click();
  await row.getByRole("button", { name: "Sí, desactivar" }).click();
  await expect(row.getByText(/Desactivada/)).toBeVisible();
  const listed = await apiDataAs<{ items: { email: string; isActive: boolean }[] }>(request, "Admin", `/Users?text=${email}`);
  expect(listed.items[0]?.isActive).toBe(false);

  await row.getByRole("button", { name: "Reactivar a Ingeniera de Prueba" }).click();
  await expect(row.getByText("Activa", { exact: true })).toBeVisible();

  // La propia cuenta no se puede desactivar.
  await page.goto("/gestion/usuarios?texto=admin@sigenrd.com");
  await expect(page.getByRole("region", { name: "Usuarios internos" }).getByText("Tu cuenta")).toBeVisible();
});

test("RF-40: la auditoría muestra los cambios y los inicios de sesión con su detalle", async ({ page, request }) => {
  // Un inicio de sesión fallido para encontrarlo.
  await request.post(`${API_URL}/Auth/login`, { data: { email: "distributor@test.energymap.local", password: "Equivocada-2026" } });
  expect((await apiAs(request, "Admin", "/AuditLogs?Action=LoginFailed")).ok()).toBe(true);

  await login(page, "Admin");
  await page.getByRole("link", { name: "Auditoría" }).first().click();
  await page.getByLabel("Acción").selectOption("LoginFailed");
  await page.getByRole("button", { name: "Filtrar" }).click();

  const table = page.getByRole("region", { name: "Registro de auditoría" });
  const failed = table.getByRole("row").filter({ hasText: "Intento de inicio fallido" }).first();
  await expect(failed).toBeVisible();
  await failed.getByText("Ver valores").click();
  await expect(failed.getByText("distributor@test.energymap.local")).toBeVisible();

  await page.goto("/gestion/auditoria?entidad=ConnectionRequest");
  await expect(table.getByRole("row").filter({ hasText: "Solicitud" }).first()).toBeVisible();
  await expect(formAlert(page, /./)).toHaveCount(0);
});

test("reportes y administración sin violaciones de accesibilidad graves, también a 360 px", async ({ page }) => {
  test.setTimeout(240_000);
  await login(page, "Admin");

  for (const viewport of [
    { width: 1280, height: 800 },
    { width: 360, height: 780 },
  ]) {
    await page.setViewportSize(viewport);
    for (const colorScheme of ["light", "dark"] as const) {
      await page.emulateMedia({ colorScheme });
      for (const path of ["/gestion/reportes", "/gestion/zonas-criticas", "/gestion/usuarios", "/gestion/auditoria"]) {
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
