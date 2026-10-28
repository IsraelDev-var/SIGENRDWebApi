import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

import { hasApi } from "./support";

// Páginas públicas de F4 que no necesitan la API para mostrarse.

test("el seguimiento muestra el formulario y está en el menú", async ({ page, isMobile }) => {
  await page.goto("/seguimiento");
  await expect(page.getByRole("heading", { level: 1, name: "Seguimiento de solicitud" })).toBeVisible();
  await expect(page.getByLabel("Número de expediente")).toBeVisible();

  if (!isMobile) await expect(page.getByRole("navigation", { name: "Principal" }).getByRole("link", { name: "Seguimiento" })).toHaveAttribute("aria-current", "page");

  const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
  expect(results.violations.filter((v) => v.impact === "serious" || v.impact === "critical").map((v) => v.id)).toEqual([]);
});

test("un número de expediente demasiado largo se rechaza sin consultar la API", async ({ page }) => {
  let apiCalled = false;
  page.on("request", (r) => {
    if (r.url().includes("/track/")) apiCalled = true;
  });

  await page.goto(`/seguimiento?expediente=${"X".repeat(60)}`);
  await expect(page.getByText("no puede pasar de 50 caracteres")).toBeVisible();
  await expect(page.getByLabel("Número de expediente")).toHaveAttribute("aria-invalid", "true");
  expect(apiCalled).toBe(false);
});

// F9-02: errores en español y dentro del sitio. Sin API, la ficha de un transformador no puede cargar sus datos.
test("si la API no responde, la página de error está en español y conserva el menú", async ({ page, isMobile }) => {
  test.skip(hasApi, "Con la API levantada la ficha sí carga");

  await page.goto("/transformadores/1");
  await expect(page.getByRole("alert").filter({ hasText: "No pudimos cargar esta página" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Reintentar" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Ir al mapa" })).toBeVisible();
  if (!isMobile) await expect(page.getByRole("navigation", { name: "Principal" })).toBeVisible();

  const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
  expect(results.violations.filter((v) => v.impact === "serious" || v.impact === "critical").map((v) => v.id)).toEqual([]);
});

test("una ruta que no existe muestra la página 404 en español", async ({ page }) => {
  const response = await page.goto("/esta-ruta-no-existe");
  expect(response?.status()).toBe(404);
  await expect(page.getByRole("heading", { level: 1, name: "No encontramos esta página" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Ir al mapa" })).toBeVisible();
});
