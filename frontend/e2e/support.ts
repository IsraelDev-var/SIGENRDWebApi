import type { APIRequestContext, Page } from "@playwright/test";

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

// ---------------- llamadas directas a la API (preparar datos sin pasar por la interfaz) ----------------

export const API_URL = `http://localhost:${process.env.E2E_API_PORT ?? 5199}/api/v1`;

/** PDF mínimo que la API acepta: firma %PDF- y sin contenido activo (DocumentFileInspector). */
export const SAMPLE_PDF = Buffer.from("%PDF-1.4\n1 0 obj<< /Type /Catalog >>endobj\ntrailer<< /Root 1 0 R >>\n%%EOF\n");

const tokens = new Map<TestRole, string>();

async function tokenFor(request: APIRequestContext, role: TestRole): Promise<string> {
  const cached = tokens.get(role);
  if (cached) return cached;
  const response = await request.post(`${API_URL}/Auth/login`, { data: { email: TEST_USERS[role], password: E2E_TEST_PASSWORD } });
  if (!response.ok()) throw new Error(`No se pudo ingresar como ${role}: ${response.status()} ${await response.text()}`);
  const token = (await response.json()).token as string;
  tokens.set(role, token);
  return token;
}

type CallOptions = { method?: "GET" | "POST" | "PUT" | "DELETE"; data?: unknown; multipart?: Record<string, string | { name: string; mimeType: string; buffer: Buffer }> };

/** Llama a la API como el rol indicado y devuelve la respuesta sin interpretar. */
export async function apiAs(request: APIRequestContext, role: TestRole, path: string, { method = "GET", data, multipart }: CallOptions = {}) {
  const headers = { Authorization: `Bearer ${await tokenFor(request, role)}` };
  return request.fetch(`${API_URL}${path}`, { method, headers, data, multipart });
}

/** Igual que apiAs, pero falla si la API no responde 2xx y devuelve `data` del sobre Response<T>. */
export async function apiDataAs<T = unknown>(request: APIRequestContext, role: TestRole, path: string, options: CallOptions = {}): Promise<T> {
  const response = await apiAs(request, role, path, options);
  if (!response.ok()) throw new Error(`${options.method ?? "GET"} ${path} como ${role}: ${response.status()} ${await response.text()}`);
  return (await response.json()).data as T;
}

/** El cliente de prueba tiene perfil (lo crea session.api.spec.ts; aquí por si se ejecuta solo). */
export async function ensureCustomerProfile(request: APIRequestContext) {
  const me = await apiAs(request, "Customer", "/Customers/me");
  if (me.status() === 404) {
    await apiDataAs(request, "Customer", "/Customers/me", {
      method: "POST",
      data: { fullName: "Cliente de prueba", nationalIdOrRnc: "001-1234567-8", isLegalEntity: false },
    });
  }
}

/** La instaladora de prueba tiene empresa y la distribuidora la validó (RN-05). */
export async function ensureValidatedInstaller(request: APIRequestContext) {
  let company = await apiAs(request, "InstallerCompany", "/InstallerCompanies/me");
  if (company.status() === 404) {
    await apiDataAs(request, "InstallerCompany", "/InstallerCompanies/me", { method: "POST", data: { tradeName: "Solar Caribe SRL", rnc: "1-31-12345-6" } });
    company = await apiAs(request, "InstallerCompany", "/InstallerCompanies/me");
  }
  const data = (await company.json()).data as { id: number; validationStatus: string };
  if (data.validationStatus !== "Validated") await apiDataAs(request, "Distributor", `/InstallerCompanies/${data.id}/validate`, { method: "POST" });
}

export type OpenTransformer = { id: number; code: string; distributorId: number; latitude: number; longitude: number };

/** Un transformador de demostración disponible cerca del Distrito Nacional. */
export async function findOpenTransformer(request: APIRequestContext, skip = 0): Promise<OpenTransformer> {
  const near = await (
    await request.get(`${API_URL}/Transformers/search?latitude=18.4861&longitude=-69.9312&radiusMeters=5000&status=Available&limit=${skip + 1}`)
  ).json();
  const id = near.data[skip].id as number;
  return (await (await request.get(`${API_URL}/Transformers/${id}`)).json()).data;
}

/** Borrador creado por la API como cliente (D-4). */
export async function createDraftAsCustomer(request: APIRequestContext, transformer: OpenTransformer, address = "Calle de prueba 1, Santo Domingo") {
  const id = await apiDataAs<number>(request, "Customer", "/ConnectionRequest", {
    method: "POST",
    data: {
      distributorId: transformer.distributorId,
      transformerId: transformer.id,
      requestType: "Connection",
      requestedCapacityKva: 5,
      usageType: "Residential",
      tariffType: "BT",
      interconnectionType: "Parallel",
      projectAddress: address,
      latitude: transformer.latitude,
      longitude: transformer.longitude,
      propertyRelation: "Owner",
      declarationAccepted: true,
    },
  });
  const { requestCode } = await apiDataAs<{ requestCode: string }>(request, "Customer", `/ConnectionRequest/${id}`);
  return { id, code: requestCode };
}

/** Sube un PDF por cada documento requerido que falte. */
export async function uploadRequiredDocuments(request: APIRequestContext, role: TestRole, id: number) {
  const required = await apiDataAs<{ items: { satisfied: boolean; acceptedTypes: string[] }[] }>(request, role, `/ConnectionRequest/${id}/required-documents`);
  for (const item of required.items.filter((i) => !i.satisfied)) {
    await apiDataAs(request, role, `/ConnectionRequest/${id}/documents`, {
      method: "POST",
      multipart: { File: { name: "documento.pdf", mimeType: "application/pdf", buffer: SAMPLE_PDF }, DocumentType: item.acceptedTypes[0]! },
    });
  }
}

/** Solicitud ya enviada (Recibida), creada por la API como cliente. */
export async function createSubmittedAsCustomer(request: APIRequestContext, transformer: OpenTransformer, address?: string) {
  const draft = await createDraftAsCustomer(request, transformer, address);
  await uploadRequiredDocuments(request, "Customer", draft.id);
  await apiDataAs(request, "Customer", `/ConnectionRequest/${draft.id}/submit`, { method: "POST" });
  return draft;
}

/** Simulación creada por la API como cliente (consumo, potencia y ubicación en el Distrito Nacional). */
export async function createSimulationAsCustomer(
  request: APIRequestContext,
  data: { name: string; monthlyConsumptionKwh: number; installedCapacityKw: number; latitude?: number; longitude?: number },
) {
  return apiDataAs<{ id: number }>(request, "Customer", "/Simulations", {
    method: "POST",
    data: { latitude: 18.4861, longitude: -69.9312, ...data, equipment: [] },
  });
}
