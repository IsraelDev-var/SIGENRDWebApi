import { expect, test, type Page } from "@playwright/test";

// F9: cabeceras de seguridad y Content Security Policy. Sin la API: páginas públicas y de acceso.

function cspViolations(page: Page) {
  const violations: string[] = [];
  page.on("console", (message) => {
    if (/Content Security Policy|Refused to (load|execute|apply|connect|create)/i.test(message.text())) violations.push(message.text());
  });
  return violations;
}

test("las páginas llevan las cabeceras de seguridad y una CSP con nonce distinto en cada petición", async ({ request }) => {
  const first = await request.get("/requisitos");
  const second = await request.get("/requisitos");
  const headers = first.headers();

  expect(headers["x-content-type-options"]).toBe("nosniff");
  expect(headers["x-frame-options"]).toBe("DENY");
  expect(headers["referrer-policy"]).toBe("strict-origin-when-cross-origin");
  expect(headers["permissions-policy"]).toContain("camera=()");
  expect(headers["cross-origin-resource-policy"]).toBe("same-origin");
  expect(headers["x-powered-by"]).toBeUndefined();

  const csp = headers["content-security-policy"] ?? "";
  expect(csp).toMatch(/script-src 'self' 'nonce-[A-Za-z0-9+/=]+' 'strict-dynamic'/);
  expect(csp).toContain("frame-ancestors 'none'");
  const nonce = (value: string) => /'nonce-([^']+)'/.exec(value)?.[1];
  expect(nonce(csp)).not.toBe(nonce(second.headers()["content-security-policy"] ?? ""));

  // Los scripts de Next llevan ese nonce.
  const html = await first.text();
  expect(html).toContain(`nonce="${nonce(csp)}"`);
});

test("el mapa y las páginas públicas funcionan sin violaciones de la CSP", async ({ page }) => {
  const violations = cspViolations(page);

  await page.goto("/");
  await expect(page.getByRole("region", { name: "Mapa de República Dominicana" })).toBeVisible();
  // El mapa pidió su estilo y dibujó el lienzo.
  await expect(page.locator("canvas.maplibregl-canvas")).toBeVisible({ timeout: 30_000 });

  for (const path of ["/transformadores", "/requisitos", "/seguimiento", "/ingresar", "/registro"]) {
    await page.goto(path);
    await expect(page.getByRole("heading", { level: 1 }).first()).toBeVisible();
  }

  // Una interacción de cliente (formulario con validación) para que corra JavaScript propio.
  await page.goto("/ingresar");
  await page.getByRole("button", { name: "Ingresar" }).click();
  await expect(page.getByText("Escribe un correo válido", { exact: false })).toBeVisible();

  expect(violations).toEqual([]);
});
