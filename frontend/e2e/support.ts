import type { Page } from "@playwright/test";

/** Contraseña de los usuarios de prueba que siembra la API en Development (igual que en playwright.config.ts). */
export const E2E_TEST_PASSWORD = "Prueba-Roles-2026!";

/** Usuarios de prueba por rol (DevelopmentTestUsers y DefaultSuperAdmin de la API). */
export const TEST_USERS = {
  Customer: "customer@test.energymap.local",
  InstallerCompany: "installer@test.energymap.local",
  Engineer: "engineer@test.energymap.local",
  Distributor: "distributor@test.energymap.local",
  Admin: "admin@sigenrd.com",
} as const;

export type TestRole = keyof typeof TEST_USERS;

/** Hay API real para esta ejecución (ver playwright.config.ts). */
export const hasApi = !!process.env.ENERGYMAP_E2E_DB;

export async function login(page: Page, role: TestRole, path = "/ingresar") {
  await page.goto(path);
  await page.getByLabel("Correo").fill(TEST_USERS[role]);
  await page.getByLabel("Contraseña").fill(E2E_TEST_PASSWORD);
  await page.getByRole("button", { name: "Ingresar" }).click();
  // Espera a que la sesión quede creada y la página cambie.
  await page.waitForURL((url) => url.pathname !== "/ingresar");
}

/** Cierra la sesión y espera a que la cookie se haya borrado (vuelve al inicio). */
export async function logout(page: Page) {
  await page.getByRole("button", { name: "Salir" }).click();
  await page.waitForURL((url) => url.pathname === "/");
}

/** Mensaje del formulario. Next tiene su propio role="alert" vacío (anunciador de rutas), por eso se filtra. */
export function formAlert(page: Page, text: string | RegExp) {
  return page.getByRole("alert").filter({ hasText: text });
}
