"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { can, type Action } from "@/config/permissions";
import { ApiError, apiData } from "@/server/api-client";
import { getSession, type Session } from "@/server/session";

/** Resultado de una acción de gestión. `errors`: la lista de la API, por ejemplo los errores por fila del CSV. */
export type ManagementActionState = {
  status: "idle" | "error" | "success";
  message?: string;
  errors?: string[];
  fieldErrors?: Record<string, string[]>;
  /** Lo que se envió, para no vaciar el formulario cuando hay errores (React lo reinicia tras cada acción). */
  values?: Record<string, string>;
};

function sent(formData: FormData): Record<string, string> {
  const values: Record<string, string> = {};
  for (const [key, value] of formData.entries()) if (typeof value === "string" && !key.startsWith("$")) values[key] = value;
  return values;
}

const NO_SESSION: ManagementActionState = { status: "error", message: "Tu sesión terminó. Vuelve a iniciar sesión para continuar." };
const FORBIDDEN: ManagementActionState = { status: "error", message: "Tu rol no puede hacer esta acción (matriz 3.2)." };

/** La interfaz ya oculta lo que el rol no puede hacer; aquí se vuelve a comprobar y la API decide. */
async function sessionFor(action: Action): Promise<Session | ManagementActionState> {
  const session = await getSession();
  if (!session) return NO_SESSION;
  return can(session.role, action) ? session : FORBIDDEN;
}

function failure(error: unknown): ManagementActionState {
  if (!(error instanceof ApiError)) throw error;
  if (error.status === 401) return NO_SESSION;
  if (error.status === 403) return FORBIDDEN;
  if (error.status === 404) return { status: "error", message: "No existe o ya no está disponible." };
  // CA-07: una transición que ya no vale (otra persona cambió el estado) llega como 409 con el motivo.
  if (error.status === 409) return { status: "error", message: `${error.message} Recarga la página para ver el estado actual.` };
  return { status: "error", message: error.errors.length > 0 ? "La API rechazó los datos." : error.message, errors: error.errors };
}

const id = (value: FormDataEntryValue | null) => {
  const n = Number(value);
  return Number.isInteger(n) && n > 0 ? n : null;
};
const decimal = (value: FormDataEntryValue | null) => {
  const text = String(value ?? "").trim();
  return /^\d+(\.\d+)?$/.test(text) ? Number(text) : null;
};
const coordinate = (value: FormDataEntryValue | null) => {
  const text = String(value ?? "").trim();
  return /^-?\d+(\.\d+)?$/.test(text) ? Number(text) : null;
};

// ---------------- revisión de solicitudes (RN-12, RN-13) ----------------

const REVIEW = {
  "start-review": { action: "reviewRequests", done: "La solicitud pasó a revisión y su capacidad quedó reservada." },
  observe: { action: "reviewRequests", done: "Observaciones enviadas. El solicitante recibió el aviso." },
  approve: { action: "decideRequests", done: "Solicitud aprobada. El certificado ya está disponible." },
  reject: { action: "decideRequests", done: "Solicitud rechazada. El solicitante recibió el motivo." },
} as const satisfies Record<string, { action: Action; done: string }>;

const OBSERVATION_TYPES = ["Technical", "Documental", "Administrative"];

export async function reviewAction(_prev: ManagementActionState, formData: FormData): Promise<ManagementActionState> {
  const transition = String(formData.get("transition") ?? "");
  if (!Object.hasOwn(REVIEW, transition)) return { status: "error", message: "La acción no es válida." };
  const step = REVIEW[transition as keyof typeof REVIEW];

  const session = await sessionFor(step.action);
  if (!("token" in session)) return session;

  const requestId = id(formData.get("id"));
  if (!requestId) return { status: "error", message: "La solicitud no es válida." };

  const comment = String(formData.get("comment") ?? "").trim();
  const needsComment = transition === "observe" || transition === "reject";
  if (needsComment && !comment) {
    const message = transition === "observe" ? "Escribe qué debe corregir el solicitante (RN-13)." : "Escribe el motivo del rechazo (RN-13).";
    return { status: "error", message, fieldErrors: { comment: [message] }, values: sent(formData) };
  }
  if (comment.length > 500) {
    return { status: "error", message: "Máximo 500 caracteres.", fieldErrors: { comment: ["Máximo 500 caracteres."] }, values: sent(formData) };
  }

  const observationType = String(formData.get("observationType") ?? "Technical");
  const body =
    transition === "observe"
      ? { id: requestId, comment, observationType: OBSERVATION_TYPES.includes(observationType) ? observationType : "Technical" }
      : transition === "reject"
        ? { id: requestId, comment }
        : undefined;

  try {
    await apiData(`/ConnectionRequest/${requestId}/${transition}`, { method: "POST", body, token: session.token });
  } catch (error) {
    return failure(error);
  }

  revalidatePath(`/gestion/solicitudes/${requestId}`);
  revalidatePath("/gestion/solicitudes");
  return { status: "success", message: step.done };
}

// ---------------- instaladoras (RN-05) ----------------

export async function validateInstallerAction(_prev: ManagementActionState, formData: FormData): Promise<ManagementActionState> {
  const session = await sessionFor("validateInstallers");
  if (!("token" in session)) return session;
  const companyId = id(formData.get("id"));
  if (!companyId) return { status: "error", message: "La empresa no es válida." };

  try {
    await apiData(`/InstallerCompanies/${companyId}/validate`, { method: "POST", token: session.token });
  } catch (error) {
    return failure(error);
  }
  revalidatePath("/gestion/instaladoras");
  return { status: "success", message: "Empresa validada: ya puede crear solicitudes." };
}

// ---------------- transformadores ----------------

function capacityErrors(total: number | null, available: number | null) {
  const errors: Record<string, string[]> = {};
  if (total === null || total <= 0) errors.totalCapacityKva = ["Escribe la capacidad total en kVA, mayor que cero."];
  if (available === null) errors.availableCapacityKva = ["Escribe la capacidad disponible en kVA (0 o más)."];
  else if (total !== null && available > total) errors.availableCapacityKva = ["La disponible no puede superar la total."];
  return errors;
}

/** Informa la capacidad vigente; el estado lo recalcula la API (RN-01, RN-02). */
export async function updateCapacityAction(_prev: ManagementActionState, formData: FormData): Promise<ManagementActionState> {
  const session = await sessionFor("manageCatalogs");
  if (!("token" in session)) return session;
  const transformerId = id(formData.get("id"));
  const total = decimal(formData.get("totalCapacityKva"));
  const available = decimal(formData.get("availableCapacityKva"));
  const fieldErrors = capacityErrors(total, available);
  if (!transformerId || Object.keys(fieldErrors).length > 0) {
    return { status: "error", message: "Revisa los campos marcados.", fieldErrors, values: sent(formData) };
  }

  try {
    await apiData(`/Transformers/${transformerId}/capacity`, {
      method: "PUT",
      body: { id: transformerId, totalCapacityKva: total, availableCapacityKva: available },
      token: session.token,
    });
  } catch (error) {
    return { ...failure(error), values: sent(formData) };
  }
  revalidatePath(`/gestion/transformadores/${transformerId}`);
  revalidatePath("/gestion/transformadores");
  return { status: "success", message: "Capacidad actualizada. El estado se recalculó." };
}

export async function setMaintenanceAction(_prev: ManagementActionState, formData: FormData): Promise<ManagementActionState> {
  const session = await sessionFor("manageCatalogs");
  if (!("token" in session)) return session;
  const transformerId = id(formData.get("id"));
  if (!transformerId) return { status: "error", message: "El transformador no es válido." };
  const inMaintenance = formData.get("inMaintenance") === "true";

  try {
    await apiData(`/Transformers/${transformerId}/maintenance`, { method: "PUT", body: { id: transformerId, inMaintenance }, token: session.token });
  } catch (error) {
    return failure(error);
  }
  revalidatePath(`/gestion/transformadores/${transformerId}`);
  revalidatePath("/gestion/transformadores");
  return {
    status: "success",
    message: inMaintenance ? "En mantenimiento: no acepta solicitudes nuevas." : "Fuera de mantenimiento: el estado se recalculó con su capacidad.",
  };
}

export async function createTransformerAction(_prev: ManagementActionState, formData: FormData): Promise<ManagementActionState> {
  const session = await sessionFor("manageCatalogs");
  if (!("token" in session)) return session;

  const code = String(formData.get("code") ?? "").trim();
  const distributorId = id(formData.get("distributorId"));
  const serviceZoneId = id(formData.get("serviceZoneId"));
  const total = decimal(formData.get("totalCapacityKva"));
  const available = decimal(formData.get("availableCapacityKva"));
  const latitude = coordinate(formData.get("latitude"));
  const longitude = coordinate(formData.get("longitude"));

  const fieldErrors = capacityErrors(total, available);
  if (!code || code.length > 50) fieldErrors.code = ["Escribe el código (máximo 50 caracteres)."];
  if (!distributorId) fieldErrors.distributorId = ["Elige la distribuidora."];
  if (!serviceZoneId) fieldErrors.serviceZoneId = ["Elige la zona."];
  if (latitude === null || latitude < -90 || latitude > 90) fieldErrors.latitude = ["Escribe la latitud, por ejemplo 18.4861."];
  if (longitude === null || longitude < -180 || longitude > 180) fieldErrors.longitude = ["Escribe la longitud, por ejemplo -69.9312."];
  if (Object.keys(fieldErrors).length > 0) return { status: "error", message: "Revisa los campos marcados.", fieldErrors, values: sent(formData) };

  let newId: number;
  try {
    newId = await apiData<number>("/Transformers", {
      method: "POST",
      body: { code, distributorId, serviceZoneId, totalCapacityKva: total, availableCapacityKva: available, latitude, longitude },
      token: session.token,
    });
  } catch (error) {
    return { ...failure(error), values: sent(formData) };
  }
  revalidatePath("/gestion/transformadores");
  redirect(`/gestion/transformadores/${newId}?creado=1`);
}

const MAX_CSV_BYTES = 5 * 1024 * 1024;

/** Importación CSV, todo o nada. Si falla, devuelve los errores de la API ("Línea N: …") para mostrarlos por fila. */
export async function importTransformersAction(_prev: ManagementActionState, formData: FormData): Promise<ManagementActionState> {
  const session = await sessionFor("manageCatalogs");
  if (!("token" in session)) return session;

  const distributorId = id(formData.get("distributorId"));
  const file = formData.get("file");
  if (!distributorId) return { status: "error", message: "Elige la distribuidora." };
  if (!(file instanceof File) || file.size === 0) return { status: "error", message: "Elige el archivo CSV." };
  if (!file.name.toLowerCase().endsWith(".csv")) return { status: "error", message: "El archivo debe tener extensión .csv." };
  if (file.size > MAX_CSV_BYTES) return { status: "error", message: "El archivo supera el máximo de 5 MB." };

  const upload = new FormData();
  upload.set("distributorId", String(distributorId));
  upload.set("file", file);

  let imported: { imported: number };
  try {
    imported = await apiData<{ imported: number }>("/Transformers/import", { method: "POST", body: upload, token: session.token });
  } catch (error) {
    const state = failure(error);
    return state.errors?.length ? { ...state, message: "No se importó nada: corrige el archivo y vuelve a subirlo." } : state;
  }
  revalidatePath("/gestion/transformadores");
  return { status: "success", message: `${imported.imported.toLocaleString("es-DO")} transformadores importados.` };
}
