import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

import { API_URL, apiAs, apiDataAs, createSubmittedAsCustomer, E2E_TEST_PASSWORD, ensureCustomerProfile, findOpenTransformer, formAlert, hasApi, login, TEST_USERS } from "./support";

// F9: notificaciones, perfil, cambio de contraseña y sesión que deja de valer. Cambian la contraseña y desactivan usuarios
// de prueba compartidos: corren en el proyecto "con-api-cierre", después de todos los demás.
test.skip(!hasApi, "Define ENERGYMAP_E2E_DB para ejecutar las pruebas contra la API");

test.beforeAll(async ({ request }) => {
  await ensureCustomerProfile(request);
  // Cada solicitud enviada deja una notificación al cliente ("recibida"): dos, para marcar una y después el resto.
  const transformer = await findOpenTransformer(request, 6);
  await createSubmittedAsCustomer(request, transformer, "Calle de las notificaciones 1");
  await createSubmittedAsCustomer(request, transformer, "Calle de las notificaciones 2");
});

test("RF-32: la campana cuenta las notificaciones sin leer y se marcan como leídas", async ({ page, request }) => {
  const unread = await apiDataAs<number>(request, "Customer", "/Notifications/unread-count");
  expect(unread).toBeGreaterThan(1);

  await login(page, "Customer");
  const bell = page.getByRole("link", { name: /^Notificaciones: \d+ sin leer$/ });
  await expect(bell).toBeVisible();
  await bell.click();
  await expect(page).toHaveURL("/portal/notificaciones");

  await page.getByRole("link", { name: "Sin leer", exact: true }).click();
  const first = page.getByRole("listitem").filter({ hasText: "(sin leer)" }).first();
  await expect(first).toBeVisible();
  await first.getByRole("button", { name: /^Marcar como leída/ }).click();
  await expect.poll(() => apiDataAs<number>(request, "Customer", "/Notifications/unread-count")).toBe(unread - 1);

  await page.getByRole("button", { name: "Marcar todas como leídas" }).click();
  await expect(page.getByText("No tienes notificaciones sin leer")).toBeVisible();
  await page.reload();
  await expect(page.getByRole("link", { name: "Notificaciones", exact: true })).toBeVisible();
});

test("RF-09: el cliente edita su contacto; la cédula no se edita aquí", async ({ page, request }) => {
  const phone = `809-555-${Date.now().toString().slice(-4)}`;
  await login(page, "Customer", "/ingresar?next=/portal/perfil");
  await expect(page.getByText("001-1234567-8")).toBeVisible();
  await expect(page.getByLabel("Cédula o RNC")).toHaveCount(0);

  await page.getByLabel("Teléfono (opcional)").fill(phone);
  await page.getByLabel("Correo de contacto (opcional)").fill("no-es-correo");
  await page.getByRole("button", { name: "Guardar cambios" }).click();
  await expect(page.getByText("Escribe un correo válido", { exact: false })).toBeVisible();
  await expect(page.getByLabel("Teléfono (opcional)")).toHaveValue(phone);

  await page.getByLabel("Correo de contacto (opcional)").fill("cliente@correo.do");
  await page.getByRole("button", { name: "Guardar cambios" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Perfil actualizado." })).toBeVisible();
  expect((await apiDataAs<{ phone: string }>(request, "Customer", "/Customers/me")).phone).toBe(phone);
});

test("cambiar la contraseña: pide la actual y deja entrar con la nueva", async ({ page, request }) => {
  const newPassword = "Otra-Clave-E2E-2026";
  await login(page, "Customer");
  await page.getByRole("link", { name: "Cambiar contraseña" }).click();

  await page.getByLabel("Contraseña actual").fill("Equivocada-2026");
  await page.getByLabel("Contraseña nueva", { exact: true }).fill(newPassword);
  await page.getByLabel("Repite la contraseña nueva").fill(newPassword);
  await page.getByRole("button", { name: "Cambiar contraseña" }).click();
  await expect(formAlert(page, "La contraseña actual no es correcta.")).toBeVisible();

  await page.getByLabel("Contraseña actual").fill(E2E_TEST_PASSWORD);
  await page.getByLabel("Contraseña nueva", { exact: true }).fill(newPassword);
  await page.getByLabel("Repite la contraseña nueva").fill(newPassword);
  await page.getByRole("button", { name: "Cambiar contraseña" }).click();
  await expect(page).toHaveURL("/portal?clave=1");

  const old = await request.post(`${API_URL}/Auth/login`, { data: { email: TEST_USERS.Customer, password: E2E_TEST_PASSWORD } });
  expect(old.status()).toBe(400);
  const fresh = await request.post(`${API_URL}/Auth/login`, { data: { email: TEST_USERS.Customer, password: newPassword } });
  expect(fresh.ok()).toBe(true);

  // Se deja como estaba para el resto de las pruebas.
  const token = (await fresh.json()).token as string;
  const restore = await request.post(`${API_URL}/Auth/change-password`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { currentPassword: newPassword, newPassword: E2E_TEST_PASSWORD },
  });
  expect(restore.ok(), await restore.text()).toBe(true);
});

test("B-5: si un administrador desactiva la cuenta, la sesión abierta se cierra con un aviso", async ({ page, request }) => {
  const engineer = (await apiDataAs<{ items: { id: string; email: string }[] }>(request, "Admin", `/Users?text=${TEST_USERS.Engineer}`)).items[0]!;

  await login(page, "Engineer");
  await expect(page.getByRole("heading", { level: 1, name: "Panel de gestión" })).toBeVisible();

  await apiDataAs(request, "Admin", `/Users/${engineer.id}/deactivate`, { method: "POST" });
  try {
    await page.goto("/gestion/solicitudes");
    await expect(page).toHaveURL(/\/ingresar\?motivo=expirada/);
    await expect(page.getByText("Tu sesión terminó. Vuelve a ingresar para continuar.")).toBeVisible();

    // Al intentar entrar, el motivo.
    await page.getByLabel("Correo").fill(TEST_USERS.Engineer);
    await page.getByLabel("Contraseña").fill(E2E_TEST_PASSWORD);
    await page.getByRole("button", { name: "Ingresar" }).click();
    await expect(formAlert(page, "desactivada")).toBeVisible();
  } finally {
    await apiAs(request, "Admin", `/Users/${engineer.id}/reactivate`, { method: "POST" });
  }
});

test("cuenta y notificaciones sin violaciones de accesibilidad graves, también a 360 px", async ({ page }) => {
  test.setTimeout(120_000);
  await login(page, "Customer");
  for (const viewport of [
    { width: 1280, height: 800 },
    { width: 360, height: 780 },
  ]) {
    await page.setViewportSize(viewport);
    for (const colorScheme of ["light", "dark"] as const) {
      await page.emulateMedia({ colorScheme });
      for (const path of ["/portal/notificaciones", "/portal/perfil", "/cambiar-contrasena"]) {
        await page.goto(path);
        await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
        const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
        const serious = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");
        expect(serious.map((v) => `${viewport.width} ${colorScheme} ${path} ${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`)).toEqual([]);
        expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth), `${viewport.width} ${path}`).toBe(false);
      }
    }
  }
});
