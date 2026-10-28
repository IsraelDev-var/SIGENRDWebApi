import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

import {
  apiAs,
  apiDataAs,
  createDraftAsCustomer,
  createSubmittedAsCustomer,
  ensureCustomerProfile,
  ensureValidatedInstaller,
  findOpenTransformer,
  formAlert,
  hasApi,
  login,
  SAMPLE_PDF,
  type OpenTransformer,
} from "./support";

// F5: solicitudes (asistente, documentos, expediente, corrección, cancelación y certificado) contra la API real.
// Corre después del proyecto "con-api" (playwright.config.ts): allí se prueba el primer ingreso de estos usuarios.
test.skip(!hasApi, "Define ENERGYMAP_E2E_DB para ejecutar las pruebas contra la API");

let transformer: OpenTransformer;

test.beforeAll(async ({ request }) => {
  await ensureCustomerProfile(request);
  await ensureValidatedInstaller(request);
  transformer = await findOpenTransformer(request);
});

function requirements(page: Page) {
  return page.getByRole("region", { name: "Documentos requeridos" }).getByRole("listitem");
}

/** Sube un PDF en cada requisito pendiente, desde la interfaz. */
async function uploadAllRequired(page: Page) {
  const pending = requirements(page).filter({ has: page.getByLabel(/^Archivo para/) });
  let count = await pending.count();
  expect(count).toBeGreaterThan(0);
  while (count > 0) {
    const item = pending.first();
    await item.getByLabel(/^Archivo para/).setInputFiles({ name: "documento.pdf", mimeType: "application/pdf", buffer: SAMPLE_PDF });
    await item.getByRole("button", { name: "Subir" }).click();
    await expect(pending).toHaveCount(count - 1);
    count--;
  }
}

test("CA-05: el cliente crea un borrador con el asistente, sube los documentos y lo envía", async ({ page, request }) => {
  await login(page, "Customer");

  // Desde la ficha del transformador.
  await page.goto(`/transformadores/${transformer.id}`);
  await page.getByRole("link", { name: /Solicitar conexión/ }).click();
  await expect(page).toHaveURL(`/portal/solicitudes/nueva?transformador=${transformer.id}`);
  await expect(page.getByRole("heading", { name: "Paso 1 de 4: Trámite" })).toBeVisible();

  // Cada paso valida antes de avanzar.
  await page.getByRole("button", { name: "Siguiente" }).click();
  await expect(page.getByText("Escribe la capacidad en kVA, por ejemplo 7.5.")).toBeVisible();
  await page.getByLabel("Capacidad requerida (kVA)").fill("6.5");
  await page.getByRole("button", { name: "Siguiente" }).click();

  await expect(page.getByRole("heading", { name: "Paso 2 de 4: Proyecto" })).toBeFocused();
  await page.getByLabel("Dirección del proyecto").fill("Av. Winston Churchill 100, Santo Domingo");
  await page.getByRole("button", { name: "Siguiente" }).click();

  // Hasta 10 kVA el ingeniero es opcional.
  await expect(page.getByRole("heading", { name: "Paso 3 de 4: Datos técnicos" })).toBeVisible();
  await page.getByRole("button", { name: "Siguiente" }).click();

  await expect(page.getByRole("heading", { name: "Paso 4 de 4: Confirmar" })).toBeVisible();
  await page.getByRole("button", { name: "Guardar borrador" }).click();
  await expect(page.getByText("Debes certificar que la información es correcta.")).toBeVisible();
  await page.getByLabel(/Certifico que la información es correcta/).check();
  await page.getByRole("button", { name: "Guardar borrador" }).click();

  // Expediente del borrador: sin documentos no se puede enviar.
  await expect(page).toHaveURL(/\/portal\/solicitudes\/\d+\?guardado=1$/);
  const id = Number(new URL(page.url()).pathname.split("/").pop());
  await expect(page.getByText("Borrador guardado.")).toBeVisible();
  await expect(page.locator("[data-status=Draft]")).toBeVisible();
  await expect(page.getByRole("button", { name: "Enviar a Edesur" })).toBeDisabled();

  // D-4: el borrador no lo ve el personal ni aparece en el seguimiento público.
  expect((await apiAs(request, "Engineer", `/ConnectionRequest/${id}`)).status()).toBe(404);

  await uploadAllRequired(page);
  await expect(page.getByRole("region", { name: "Documentos del expediente" }).getByText("documento.pdf").first()).toBeVisible();

  await page.getByRole("button", { name: "Enviar a Edesur" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Solicitud enviada" })).toBeVisible();
  await expect(page.locator("[data-status=Received]")).toBeVisible();
  await expect(page.getByRole("region", { name: "Historial" }).getByText("Recibida")).toBeVisible();

  // Ahora sí la ve el personal, con lo que escribió el cliente.
  const staffView = await apiDataAs<{ status: string; requestedCapacityKva: number; projectAddress: string }>(request, "Engineer", `/ConnectionRequest/${id}`);
  expect(staffView).toMatchObject({ status: "Received", requestedCapacityKva: 6.5, projectAddress: "Av. Winston Churchill 100, Santo Domingo" });

  // Y aparece en su lista.
  await page.getByRole("link", { name: "Mis solicitudes" }).first().click();
  await expect(page.getByRole("region", { name: "Mis solicitudes" }).getByRole("row").filter({ hasText: "Recibida" }).first()).toBeVisible();
});

test("CA-06: rechaza un .exe y un archivo de más de 10 MB antes de enviarlos, y la API rechaza un PDF falso", async ({ page, request }) => {
  const { id } = await createDraftAsCustomer(request, transformer, "Calle de los archivos 2");
  await login(page, "Customer", `/ingresar?next=/portal/solicitudes/${id}`);

  const item = requirements(page).first();
  const input = item.getByLabel(/^Archivo para/);

  await input.setInputFiles({ name: "instalador.exe", mimeType: "application/x-msdownload", buffer: Buffer.from("MZ") });
  await expect(item.getByText("Solo se aceptan archivos PDF, JPG o PNG.")).toBeVisible();
  await item.getByRole("button", { name: "Subir" }).click();
  await expect(item.getByText("Solo se aceptan archivos PDF, JPG o PNG.")).toBeVisible();

  const big = Buffer.alloc(10 * 1024 * 1024 + 1, 0x20);
  SAMPLE_PDF.copy(big);
  await input.setInputFiles({ name: "plano.pdf", mimeType: "application/pdf", buffer: big });
  await expect(item.getByText(/pasa de 10 MB/)).toBeVisible();
  await item.getByRole("button", { name: "Subir" }).click();

  // Ninguno llegó a la API.
  expect(await apiDataAs<unknown[]>(request, "Customer", `/ConnectionRequest/${id}/documents`)).toEqual([]);

  // Con extensión .pdf pero sin contenido PDF: lo detecta la API por la firma del archivo (RN-18).
  await input.setInputFiles({ name: "falso.pdf", mimeType: "application/pdf", buffer: Buffer.from("no soy un pdf") });
  await item.getByRole("button", { name: "Subir" }).click();
  await expect(formAlert(page, "Formato no permitido")).toBeVisible();
  expect(await apiDataAs<unknown[]>(request, "Customer", `/ConnectionRequest/${id}/documents`)).toEqual([]);
});

test("CA-08: una solicitud aprobada no se puede modificar y su certificado se descarga", async ({ page, request }) => {
  const { id } = await createSubmittedAsCustomer(request, transformer, "Calle aprobada 3");
  await apiDataAs(request, "Distributor", `/ConnectionRequest/${id}/start-review`, { method: "POST" });
  await apiDataAs(request, "Distributor", `/ConnectionRequest/${id}/approve`, { method: "POST" });

  await login(page, "Customer", `/ingresar?next=/portal/solicitudes/${id}`);
  await expect(page.locator("[data-status=Approved]")).toBeVisible();

  // Nada que cambie el expediente (RN-07, RN-19).
  await expect(page.getByLabel(/^Archivo para/)).toHaveCount(0);
  await expect(page.getByRole("button", { name: /^Quitar/ })).toHaveCount(0);
  await expect(page.getByRole("link", { name: "Editar los datos" })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /Cancelar la solicitud/ })).toHaveCount(0);
  await page.goto(`/portal/solicitudes/${id}/editar`);
  await expect(page).toHaveURL(`/portal/solicitudes/${id}`);
  expect((await apiAs(request, "Customer", `/ConnectionRequest/${id}/cancel`, { method: "POST", data: {} })).status()).toBe(409);

  const downloadPromise = page.waitForEvent("download");
  await page.getByRole("link", { name: "Descargar el certificado de aprobación" }).click();
  const download = await downloadPromise;
  expect(download.suggestedFilename()).toMatch(/\.pdf$/);
  const stream = await download.createReadStream();
  const chunks: Buffer[] = [];
  for await (const chunk of stream) chunks.push(chunk as Buffer);
  expect(Buffer.concat(chunks).subarray(0, 5).toString()).toBe("%PDF-");
});

test("RN-22: otro usuario no ve el expediente ni descarga sus documentos", async ({ page, request }) => {
  const { id } = await createSubmittedAsCustomer(request, transformer, "Calle privada 4");
  const [document] = await apiDataAs<{ id: number }[]>(request, "Customer", `/ConnectionRequest/${id}/documents`);

  await login(page, "InstallerCompany", `/ingresar?next=/portal/solicitudes/${id}`);
  await expect(page.getByRole("heading", { name: "No encontramos esa solicitud" })).toBeVisible();
  await expect(page.getByText("Calle privada 4")).toHaveCount(0);

  const download = await page.request.get(`/portal/solicitudes/${id}/documentos/${document!.id}`);
  expect(download.status()).toBe(404);
  expect(await download.text()).not.toContain("%PDF");

  const list = await page.request.get(`/portal/solicitudes/${id}/certificado`);
  expect(list.status()).toBe(404);
});

test("B-11: la instaladora crea una solicitud a nombre de un cliente", async ({ page, request }) => {
  await login(page, "InstallerCompany");
  await page.goto(`/portal/solicitudes/nueva?transformador=${transformer.id}`);

  await expect(page.getByRole("heading", { name: "Paso 1 de 5: Trámite" })).toBeVisible();
  await page.getByLabel("Capacidad requerida (kVA)").fill("4");
  await page.getByRole("button", { name: "Siguiente" }).click();

  await expect(page.getByRole("heading", { name: "Paso 2 de 5: Titular" })).toBeVisible();
  await page.getByRole("button", { name: "Siguiente" }).click();
  await expect(page.getByText("Escribe la cédula o el RNC del titular.")).toBeVisible();
  const nationalId = `402${Date.now().toString().slice(-8)}`;
  await page.getByLabel("Nombre del titular").fill("María Titular");
  await page.getByLabel("Cédula o RNC del titular").fill(nationalId);
  await page.getByRole("button", { name: "Siguiente" }).click();

  await page.getByLabel("Dirección del proyecto").fill("Calle de la instaladora 5");
  await page.getByRole("button", { name: "Siguiente" }).click();
  await page.getByRole("button", { name: "Siguiente" }).click();
  await page.getByLabel(/Certifico que la información es correcta/).check();
  await page.getByRole("button", { name: "Guardar borrador" }).click();

  await expect(page).toHaveURL(/\/portal\/solicitudes\/\d+\?guardado=1$/);
  const id = Number(new URL(page.url()).pathname.split("/").pop());
  const data = page.getByRole("region", { name: "Datos de la solicitud" });
  await expect(data.getByText("María Titular")).toBeVisible();
  await expect(data.getByText("Solar Caribe SRL")).toBeVisible();

  // El cliente de prueba (otra cédula) no la ve.
  expect((await apiAs(request, "Customer", `/ConnectionRequest/${id}`)).status()).toBe(404);

  await page.getByRole("link", { name: "Mis solicitudes" }).first().click();
  await expect(page.getByRole("region", { name: "Mis solicitudes" }).getByRole("cell", { name: "María Titular" })).toBeVisible();
});

test("observaciones: el cliente ve el comentario, corrige y reenvía", async ({ page, request }) => {
  const { id } = await createSubmittedAsCustomer(request, transformer, "Calle observada 6");
  await apiDataAs(request, "Engineer", `/ConnectionRequest/${id}/start-review`, { method: "POST" });
  await apiDataAs(request, "Engineer", `/ConnectionRequest/${id}/observe`, { method: "POST", data: { comment: "La cédula está borrosa." } });

  await login(page, "Customer", `/ingresar?next=/portal/solicitudes/${id}`);
  await expect(page.getByRole("region", { name: "Lo que pidió Edesur" }).getByText("La cédula está borrosa.")).toBeVisible();

  await page.getByRole("button", { name: "Corregir la solicitud" }).click();
  await expect(page.locator("[data-status=InCorrection]")).toBeVisible();

  // En corrección puede cambiar documentos: quita uno y lo vuelve a subir.
  await page.getByRole("button", { name: /^Quitar/ }).first().click();
  await expect(page.getByRole("button", { name: "Reenviar a Edesur" })).toBeDisabled();
  await uploadAllRequired(page);

  await page.getByRole("button", { name: "Reenviar a Edesur" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Solicitud reenviada" })).toBeVisible();
  await expect(page.locator("[data-status=UnderReview]")).toBeVisible();
});

test("descartar un borrador y cancelar una solicitud piden confirmación", async ({ page, request }) => {
  const draft = await createDraftAsCustomer(request, transformer, "Calle descartada 7");
  const sent = await createSubmittedAsCustomer(request, transformer, "Calle cancelada 8");

  await login(page, "Customer", `/ingresar?next=/portal/solicitudes/${draft.id}`);
  await page.getByRole("button", { name: "Descartar borrador" }).click();
  await page.getByRole("button", { name: "No, volver" }).click();
  await expect(page.locator("[data-status=Draft]")).toBeVisible();
  await page.getByRole("button", { name: "Descartar borrador" }).click();
  await page.getByRole("button", { name: "Sí, descartar" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Borrador descartado" })).toBeVisible();
  await expect(page.locator("[data-status=Cancelled]")).toBeVisible();

  await page.goto(`/portal/solicitudes/${sent.id}`);
  await page.getByRole("button", { name: "Cancelar la solicitud" }).click();
  await page.getByLabel("Motivo (opcional)").fill("Cambié de proyecto.");
  await page.getByRole("button", { name: "Sí, cancelar la solicitud" }).click();
  await expect(page.locator("[data-status=Cancelled]")).toBeVisible();
  await expect(page.getByRole("region", { name: "Historial" }).getByText("Cambié de proyecto.")).toBeVisible();
});

test("solicitudes sin violaciones de accesibilidad graves, también a 360 px", async ({ page, request }) => {
  const { id } = await createDraftAsCustomer(request, transformer, "Calle accesible 9");
  await login(page, "Customer");

  for (const viewport of [
    { width: 1280, height: 800 },
    { width: 360, height: 780 },
  ]) {
    await page.setViewportSize(viewport);
    for (const colorScheme of ["light", "dark"] as const) {
      await page.emulateMedia({ colorScheme });
      for (const path of ["/portal/solicitudes", `/portal/solicitudes/nueva?transformador=${transformer.id}`, `/portal/solicitudes/${id}`]) {
        await page.goto(path);
        await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
        const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
        const serious = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");
        expect(serious.map((v) => `${viewport.width} ${colorScheme} ${path} ${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`)).toEqual([]);
        // Sin desplazamiento horizontal de la página (CA-13).
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth <= window.innerWidth
            ? []
            : [...document.querySelectorAll("body *")]
                .filter((el) => el.getBoundingClientRect().right > window.innerWidth + 1)
                .slice(0, 5)
                .map((el) => `${el.tagName.toLowerCase()}.${String(el.className).slice(0, 80)}`),
        );
        expect(overflow, `${viewport.width} ${path}`).toEqual([]);
      }
    }
  }
});

test("un PDF de 6 MB se sube directo del navegador a la API, sin pasar por el servidor de Next", async ({ page, request }) => {
  const { id } = await createDraftAsCustomer(request, transformer, "Calle del archivo grande 3");
  await login(page, "Customer", `/ingresar?next=/portal/solicitudes/${id}`);

  // Más que el límite de cuerpo de Vercel (4,5 MB) y menos que el de la API (10 MB, RN-18).
  const big = Buffer.alloc(6 * 1024 * 1024, 0x20);
  SAMPLE_PDF.copy(big);

  const directUpload = page.waitForRequest((r) => r.method() === "POST" && r.url().endsWith(`/api/v1/ConnectionRequest/${id}/documents/direct`));
  const item = requirements(page).first();
  await item.getByLabel(/^Archivo para/).setInputFiles({ name: "plano grande.pdf", mimeType: "application/pdf", buffer: big });
  await item.getByRole("button", { name: "Subir" }).click();

  // La petición va a la API, con el permiso y no con la cookie de sesión.
  const sent = await directUpload;
  expect(sent.headers().authorization).toMatch(/^Bearer /);
  expect(sent.headers().cookie).toBeUndefined();
  const response = await sent.response();
  expect(response?.status()).toBe(200);

  // El requisito queda cumplido (su formulario desaparece) y el documento está en el expediente.
  await expect
    .poll(() => apiDataAs<{ fileName: string; sizeBytes: number }[]>(request, "Customer", `/ConnectionRequest/${id}/documents`))
    .toEqual([expect.objectContaining({ fileName: "plano grande.pdf", sizeBytes: big.length })]);
});
