import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

import { hasApi } from "./support";

// F4: seguimiento público y requisitos, contra la API real. El seguimiento tiene un límite de 4 consultas por minuto
// por IP en estas pruebas; cada prueba manda su propia IP, que el servidor de Next reenvía a la API con la clave.
test.skip(!hasApi, "Define ENERGYMAP_E2E_DB para ejecutar las pruebas contra la API");

let nextIp = 1;
/** Una IP de documentación (RFC 5737) distinta para cada prueba. */
async function asNewVisitor(page: Page) {
  const ip = `198.51.100.${(test.info().workerIndex * 50 + nextIp++) % 250}`;
  await page.setExtraHTTPHeaders({ "X-Forwarded-For": ip });
  return ip;
}

async function track(page: Page, code: string) {
  await page.goto("/seguimiento");
  await page.getByLabel("Número de expediente").fill(code);
  await page.getByRole("button", { name: "Consultar" }).click();
}

test("un expediente que no existe lo dice, sin revelar nada más", async ({ page }) => {
  await asNewVisitor(page);
  await track(page, "req-noexiste");

  await expect(page).toHaveURL(/expediente=req-noexiste/i);
  await expect(page.getByText("No encontramos el expediente REQ-NOEXISTE")).toBeVisible();
});

test("tras muchas consultas seguidas avisa que espere, y a otra persona no la afecta", async ({ page }) => {
  await asNewVisitor(page);
  for (let i = 0; i < 4; i++) {
    await page.goto(`/seguimiento?expediente=REQ-LIMITE-${i}`);
    await expect(page.getByText(/No encontramos el expediente/)).toBeVisible();
  }

  await page.goto("/seguimiento?expediente=REQ-LIMITE-5");
  await expect(page.getByRole("alert").filter({ hasText: "Demasiadas consultas seguidas" })).toBeVisible();
  await expect(page.getByText("Hiciste muchos intentos seguidos. Espera un minuto")).toBeVisible();

  // El límite es por usuario aunque todos lleguen por el mismo servidor de Next (antes de F4 era por sitio).
  await asNewVisitor(page);
  await page.goto("/seguimiento?expediente=REQ-LIMITE-6");
  await expect(page.getByText(/No encontramos el expediente/)).toBeVisible();
});

test("los requisitos salen de la API y cambian según el trámite", async ({ page }) => {
  await page.goto("/requisitos");

  const small = page.getByRole("region", { name: "Conexión nueva de hasta 10 kVA" });
  await expect(small.getByText("Cédula o pasaporte del solicitante")).toBeVisible();
  await expect(small.getByText("Copia del RNC")).toBeVisible(); // en "si es una empresa"

  const large = page.getByRole("region", { name: "Conexión nueva de más de 10 kVA" });
  await expect(large.getByText("Carnet CODIA del ingeniero responsable")).toBeVisible();

  const netMetering = page.getByRole("region", { name: /Medición neta/ });
  await expect(netMetering.getByText(/Diagrama unifilar/)).toBeVisible();
  await expect(netMetering.getByText("Autorización y cédula del dueño del inmueble")).toBeVisible();
});

test("seguimiento y requisitos sin violaciones de accesibilidad graves", async ({ page }) => {
  for (const colorScheme of ["light", "dark"] as const) {
    await page.emulateMedia({ colorScheme });
    await asNewVisitor(page);
    for (const path of ["/seguimiento?expediente=REQ-AXE", "/requisitos"]) {
      await page.goto(path);
      await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
      const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
      const serious = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");
      expect(serious.map((v) => `${colorScheme} ${path} ${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`)).toEqual([]);
    }
  }
});
