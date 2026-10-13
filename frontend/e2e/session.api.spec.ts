import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

import { createSubmittedAsCustomer, E2E_TEST_PASSWORD, findOpenTransformer, formAlert, hasApi, login, logout, TEST_USERS, type TestRole } from "./support";

// Contra la API .NET real (ver playwright.config.ts). Criterio de F2: ingreso de los 5 roles, acceso directo
// por URL bloqueado y matriz 3.2 aplicada.
test.skip(!hasApi, "Define ENERGYMAP_E2E_DB para ejecutar las pruebas contra la API");

const HOME: Record<TestRole, string> = {
  Customer: "/portal",
  InstallerCompany: "/portal",
  Engineer: "/gestion",
  Distributor: "/gestion",
  Admin: "/gestion",
};

const LABEL: Record<TestRole, string> = {
  Customer: "Cliente",
  InstallerCompany: "Instaladora",
  Engineer: "Ingeniero",
  Distributor: "Distribuidora",
  Admin: "Administrador",
};

for (const role of Object.keys(TEST_USERS) as TestRole[]) {
  test(`${LABEL[role]}: ingresa a su zona, ve su rol y sale`, async ({ page }) => {
    await login(page, role);

    await expect(page).toHaveURL(HOME[role]);
    await expect(page.getByText(LABEL[role], { exact: true })).toBeVisible();

    await logout(page);
    await expect(page.getByRole("link", { name: "Ingresar" })).toBeVisible();

    // Sin sesión, la zona vuelve a pedir el ingreso.
    await page.goto(HOME[role]);
    await expect(page).toHaveURL(/\/ingresar\?next=/);
  });
}

test("la sesión vive en una cookie HttpOnly que el navegador no puede leer", async ({ page, context }) => {
  await login(page, "Customer");
  await expect(page).toHaveURL("/portal");

  const cookie = (await context.cookies()).find((c) => c.name === "em_session");
  expect(cookie).toMatchObject({ httpOnly: true, secure: true, sameSite: "Lax" });
  expect(await page.evaluate(() => document.cookie)).not.toContain("em_session");
});

test.describe("acceso directo por URL a la zona de otro rol", () => {
  for (const [role, path] of [
    ["Customer", "/gestion"],
    ["InstallerCompany", "/gestion"],
    ["Engineer", "/portal/perfil"],
    ["Admin", "/portal"],
  ] as [TestRole, string][]) {
    test(`${LABEL[role]} en ${path} recibe 403`, async ({ page }) => {
      await login(page, role);
      await expect(page).toHaveURL(HOME[role]);

      const response = await page.goto(path);
      expect(response?.status()).toBe(403);
      await expect(page.getByText("Esta sección no está disponible para tu cuenta")).toBeVisible();
      await expect(page).toHaveURL(path);
    });
  }
});

test("la gestión muestra a cada rol solo lo que la matriz 3.2 le permite", async ({ page }) => {
  await login(page, "Engineer");
  const engineer = page.getByTestId("permisos-del-rol");
  await expect(engineer).toContainText("Pasar a revisión y emitir observaciones");
  await expect(engineer).not.toContainText("Aprobar o rechazar");
  await expect(engineer).not.toContainText("Consultar la auditoría");

  await logout(page);
  await login(page, "Admin");
  const admin = page.getByTestId("permisos-del-rol");
  await expect(admin).toContainText("Aprobar o rechazar solicitudes");
  await expect(admin).toContainText("Gestionar usuarios internos");
  await expect(admin).toContainText("Consultar la auditoría");
});

test("después de ingresar vuelve a la página que pidió", async ({ page }) => {
  await page.goto("/portal/perfil");
  await expect(page).toHaveURL("/ingresar?next=%2Fportal%2Fperfil");

  await page.getByLabel("Correo").fill(TEST_USERS.Customer);
  await page.getByLabel("Contraseña").fill(E2E_TEST_PASSWORD);
  await page.getByRole("button", { name: "Ingresar" }).click();
  await expect(page).toHaveURL("/portal/perfil");
});

test("no redirige fuera del sitio aunque el enlace lo pida", async ({ page }) => {
  await login(page, "Customer", "/ingresar?next=https%3A%2F%2Fotro.example%2Frobar");
  await expect(page).toHaveURL("/portal");
});

test("con sesión, ingresar y registro llevan al inicio del rol", async ({ page }) => {
  await login(page, "Distributor");
  await page.goto("/ingresar");
  await expect(page).toHaveURL("/gestion");
  await page.goto("/registro");
  await expect(page).toHaveURL("/gestion");
});

test("credenciales incorrectas: mensaje sin revelar si la cuenta existe", async ({ page }) => {
  await page.goto("/ingresar");
  await page.getByLabel("Correo").fill("nadie@test.energymap.local");
  await page.getByLabel("Contraseña").fill("NoEsLaClave123");
  await page.getByRole("button", { name: "Ingresar" }).click();

  await expect(formAlert(page, "El correo o la contraseña no son correctos")).toBeVisible();
  await expect(page).toHaveURL("/ingresar");
  await expect(page.getByLabel("Correo")).toHaveValue("nadie@test.energymap.local");
});

test("cliente: completa su perfil en el primer ingreso", async ({ page }) => {
  await login(page, "Customer");
  await expect(page.getByRole("heading", { name: "Completa tu perfil" })).toBeVisible();
  await page.getByRole("link", { name: "Completar perfil" }).click();

  await page.getByRole("button", { name: "Guardar perfil" }).click();
  await expect(page.getByText("Escribe la cédula o el RNC.")).toBeVisible();

  await page.getByLabel("Cédula o RNC").fill("001-1234567-8");
  await page.getByLabel("Teléfono (opcional)").fill("809-555-0101");
  await page.getByRole("button", { name: "Guardar perfil" }).click();

  await expect(page).toHaveURL("/portal");
  await expect(page.getByRole("heading", { name: "Completa tu perfil" })).toHaveCount(0);

  await page.getByRole("link", { name: "Mi perfil" }).first().click();
  await expect(page.getByText("001-1234567-8")).toBeVisible();
  await expect(page.getByText("809-555-0101")).toBeVisible();
});

test("el cliente envía una solicitud y cualquiera la sigue con el número de expediente (RF-14)", async ({ page, request }) => {
  // Se prepara por la API: borrador, documentos y envío (D-4). El asistente se prueba en requests.api.spec.ts.
  const transformer = await findOpenTransformer(request);
  const { code } = await createSubmittedAsCustomer(request, transformer);

  // Sin sesión, solo con el número.
  await page.context().clearCookies();
  await page.setExtraHTTPHeaders({ "X-Forwarded-For": "198.51.100.251" });
  await page.goto(`/seguimiento?expediente=${code.toLowerCase()}`);
  const result = page.getByRole("region", { name: code });
  await expect(result).toBeVisible();
  await expect(result.getByText("Recibida")).toBeVisible();
  await expect(result.getByText("Conexión nueva")).toBeVisible();
  await expect(result.getByText(/Edesur la recibió y la revisará/)).toBeVisible();
  // Nada del solicitante: ni nombre ni dirección (RN-22).
  await expect(page.getByText("Calle de prueba 1")).toHaveCount(0);
});

test("instaladora: registra su empresa y queda pendiente de validación (RN-05)", async ({ page }) => {
  await login(page, "InstallerCompany");
  await page.getByRole("link", { name: "Registrar empresa" }).click();

  await page.getByLabel("Nombre comercial").fill("Solar Caribe SRL");
  await page.getByLabel("RNC").fill("1-31-12345-6");
  await page.getByRole("button", { name: "Registrar empresa" }).click();

  await expect(page).toHaveURL("/portal");
  await expect(page.getByRole("heading", { name: "Tu empresa está pendiente de validación" })).toBeVisible();
});

test("registro de una cuenta nueva", async ({ page }) => {
  const email = `nuevo-${Date.now()}@test.energymap.local`;
  await page.goto("/registro");
  await page.getByLabel("Empresa instaladora").check();
  await page.getByLabel("Nombre completo").fill("Instaladora Nueva");
  await page.getByLabel("Correo").fill(email);
  await page.getByLabel("Contraseña", { exact: true }).fill("ClaveSegura2026");
  await page.getByLabel("Repite la contraseña").fill("ClaveSegura2026");
  await page.getByRole("button", { name: "Crear cuenta" }).click();

  await expect(page.getByRole("status")).toContainText(`enviamos un enlace a ${email}`);

  // Sin confirmar el correo no puede ingresar.
  await page.goto("/ingresar");
  await page.getByLabel("Correo").fill(email);
  await page.getByLabel("Contraseña").fill("ClaveSegura2026");
  await page.getByRole("button", { name: "Ingresar" }).click();
  await expect(formAlert(page, "todavía no está confirmada")).toBeVisible();
});

test("un enlace de confirmación inválido lo dice al confirmar", async ({ page }) => {
  await page.goto("/confirmar-correo?userId=no-existe&code=QUJD");
  await page.getByRole("button", { name: "Confirmar mi correo" }).click();
  await expect(formAlert(page, "no es válido")).toBeVisible();
});

test("portal y gestión sin violaciones de accesibilidad graves", async ({ page }) => {
  for (const [role, path] of [
    ["Customer", "/portal/perfil"],
    ["Engineer", "/gestion"],
  ] as [TestRole, string][]) {
    await page.context().clearCookies();
    await login(page, role, `/ingresar?next=${encodeURIComponent(path)}`);
    await expect(page).toHaveURL(path);

    const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
    const serious = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");
    expect(serious.map((v) => `${path} ${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`)).toEqual([]);
  }
});
