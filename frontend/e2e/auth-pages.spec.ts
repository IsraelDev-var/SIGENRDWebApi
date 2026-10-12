import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

import { formAlert } from "./support";

// Páginas de acceso sin API: se renderizan, son accesibles y validan en el servidor antes de llamar a la API.

const PAGES = [
  { path: "/ingresar", heading: "Ingresar a EnergyMap" },
  { path: "/registro", heading: "Crear una cuenta" },
  { path: "/recuperar", heading: "Recuperar la contraseña" },
  { path: "/restablecer?email=a%40b.com&code=X", heading: "Crear una contraseña nueva" },
  { path: "/confirmar-correo?userId=u&code=c", heading: "Confirma tu correo" },
];

for (const colorScheme of ["light", "dark"] as const) {
  test.describe(`páginas de acceso, tema ${colorScheme === "light" ? "claro" : "oscuro"}`, () => {
    test.use({ colorScheme });

    for (const { path, heading } of PAGES) {
      test(`${path.split("?")[0]} sin violaciones de accesibilidad graves`, async ({ page }) => {
        await page.goto(path);
        await expect(page.getByRole("heading", { level: 1, name: heading })).toBeVisible();

        const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
        const serious = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");
        expect(serious.map((v) => `${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`)).toEqual([]);
      });
    }
  });
}

test("el ingreso valida los campos y asocia cada error a su campo", async ({ page }) => {
  await page.goto("/ingresar");
  await page.getByRole("button", { name: "Ingresar" }).click();

  const email = page.getByLabel("Correo");
  await expect(email).toHaveAttribute("aria-invalid", "true");
  await expect(page.getByText("Escribe un correo válido")).toBeVisible();
  await expect(page.getByText("Escribe tu contraseña.")).toBeVisible();
  await expect(formAlert(page, "Revisa los campos marcados")).toBeVisible();
});

test("el registro exige la política de contraseñas de la API", async ({ page }) => {
  await page.goto("/registro");
  await page.getByLabel("Nombre completo").fill("Ana Pérez");
  await page.getByLabel("Correo").fill("ana@example.com");
  await page.getByLabel("Contraseña", { exact: true }).fill("corta");
  await page.getByLabel("Repite la contraseña").fill("otra");
  await page.getByRole("button", { name: "Crear cuenta" }).click();

  await expect(page.getByText("La contraseña debe tener al menos 10 caracteres.")).toBeVisible();
  // Solo el primer error de la contraseña, no las cuatro reglas a la vez.
  await expect(page.getByText("al menos una mayúscula")).toHaveCount(0);
  await expect(page.getByText("Las contraseñas no coinciden.")).toBeVisible();
  // Lo escrito se conserva, salvo las contraseñas.
  await expect(page.getByLabel("Nombre completo")).toHaveValue("Ana Pérez");
  await expect(page.getByLabel("Contraseña", { exact: true })).toHaveValue("");
});

test.describe("zonas privadas sin sesión", () => {
  for (const path of ["/portal", "/portal/perfil", "/gestion"]) {
    test(`${path} lleva a ingresar y recuerda a dónde volver`, async ({ page }) => {
      await page.goto(path);
      await expect(page).toHaveURL(`/ingresar?next=${encodeURIComponent(path)}`);
    });
  }

  test("una cookie de sesión falsificada no da acceso", async ({ page, context, baseURL }) => {
    await context.addCookies([{ name: "em_session", value: "eyJhbGciOiJub25lIn0.eyJyb2xlIjoiQWRtaW4ifQ.", url: baseURL! }]);
    await page.goto("/gestion");
    await expect(page).toHaveURL(/\/ingresar\?next=%2Fgestion$/);
  });
});

test("los enlaces incompletos de los correos lo dicen", async ({ page }) => {
  await page.goto("/confirmar-correo");
  await expect(page.getByText("El enlace está incompleto")).toBeVisible();

  await page.goto("/restablecer?email=a%40b.com");
  await expect(page.getByText("El enlace está incompleto")).toBeVisible();
});
