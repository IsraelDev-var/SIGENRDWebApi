import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

// Segundo servidor sin la variable del catálogo: solo existe en CI (ver playwright.config.ts).
const catalogOffUrl = process.env.CI ? `http://localhost:${Number(process.env.PORT ?? 3000) + 1}` : null;

const PAGES = [
  { path: "/catalogo", heading: "Catálogo de componentes" },
  { path: "/catalogo/publica", heading: "Zona pública: aquí va el mapa" },
  { path: "/catalogo/portal", heading: "Mis solicitudes" },
  { path: "/catalogo/gestion", heading: "Bandeja de revisión" },
];

for (const colorScheme of ["light", "dark"] as const) {
  test.describe(`Design System, tema ${colorScheme === "light" ? "claro" : "oscuro"}`, () => {
    test.use({ colorScheme });

    for (const { path, heading } of PAGES) {
      test(`${path} sin violaciones de accesibilidad graves`, async ({ page }) => {
        await page.goto(path);
        await expect(page.getByRole("heading", { level: 1, name: heading })).toBeVisible();

        const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
        const serious = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");

        expect(serious.map((v) => `${v.id}: ${v.help} → ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`)).toEqual([]);
      });
    }
  });
}

test("el tema oscuro cambia de verdad los colores", async ({ browser }) => {
  const background = async (colorScheme: "light" | "dark") => {
    const page = await browser.newPage({ colorScheme });
    await page.goto("/catalogo");
    const value = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
    await page.close();
    return value;
  };

  expect(await background("light")).not.toBe(await background("dark"));
});

test("el primer Tab lleva a 'Saltar al contenido'", async ({ page }) => {
  await page.goto("/catalogo/gestion");
  await page.keyboard.press("Tab");

  const skip = page.getByRole("link", { name: "Saltar al contenido" });
  await expect(skip).toBeFocused();
  await skip.press("Enter");
  await expect(page).toHaveURL(/#contenido$/);
});

test("el menú marca la página actual", async ({ page, isMobile }) => {
  test.skip(isMobile, "En móvil el menú está en el panel lateral");
  await page.goto("/catalogo/gestion");

  const nav = page.getByRole("navigation", { name: "Gestión" });
  await expect(nav.getByRole("link", { name: "Solicitudes" })).toHaveAttribute("aria-current", "page");
  // Solo uno: "Panel" (/catalogo/gestion) también coincide por prefijo y no debe marcarse.
  await expect(nav.locator('[aria-current="page"]')).toHaveCount(1);
});

test("en móvil el menú se abre en un panel", async ({ page, isMobile }) => {
  test.skip(!isMobile, "Solo en el proyecto móvil");
  await page.goto("/catalogo/gestion");

  await page.getByRole("button", { name: "Abrir menú" }).click();
  const dialog = page.getByRole("dialog", { name: "Gestión" });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByRole("link", { name: "Transformadores" })).toBeVisible();
});

test("el tooltip se muestra al enfocar con el teclado", async ({ page }) => {
  await page.goto("/catalogo");
  await page.getByRole("button", { name: "kVA" }).focus();

  await expect(page.getByRole("tooltip")).toContainText("Kilovoltio-amperio");
});

test("sin ENERGYMAP_DESIGN_CATALOG el catálogo responde 404", async ({ request }) => {
  test.skip(!catalogOffUrl, "Solo en CI, donde corre un segundo servidor sin la variable");

  expect((await request.get(`${catalogOffUrl}/catalogo`)).status()).toBe(404);
  expect((await request.get(`${catalogOffUrl}/catalogo/gestion`)).status()).toBe(404);
  expect((await request.get(`${catalogOffUrl}/`)).status()).toBe(200);
});
