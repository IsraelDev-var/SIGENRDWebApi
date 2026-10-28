"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { can } from "@/config/permissions";
import { ApiError, apiData } from "@/server/api-client";
import { serverEnv } from "@/server/env";
import { getSession, type Session } from "@/server/session";

import { parseRequestForm, type FieldErrors, type RequestFormValues } from "./schemas";

/** Resultado de una acción sobre una solicitud. */
export type RequestActionState = {
  status: "idle" | "error" | "success";
  message?: string;
  fieldErrors?: FieldErrors;
};

const NO_SESSION: RequestActionState = { status: "error", message: "Tu sesión terminó. Vuelve a iniciar sesión para continuar." };

/** La interfaz ya oculta estas acciones a otros roles; aquí se vuelve a comprobar, y la API decide. */
async function applicantSession(): Promise<Session | null> {
  const session = await getSession();
  return session && can(session.role, "createRequest") ? session : null;
}

function failure(error: unknown): RequestActionState {
  if (error instanceof ApiError) {
    if (error.status === 401) return NO_SESSION;
    if (error.status === 404) return { status: "error", message: "La solicitud no existe o no es tuya." };
    if (error.status === 409) return { status: "error", message: `${error.message} Recarga la página para ver su estado actual.` };
    return { status: "error", message: error.errors.length > 0 ? error.errors.join(" ") : error.message };
  }
  throw error;
}

function parseId(value: FormDataEntryValue | number | null | undefined): number | null {
  const id = Number(value);
  return Number.isInteger(id) && id > 0 ? id : null;
}

/** Crea el borrador (D-4) o guarda los cambios de uno existente, y lleva al expediente. */
export async function saveDraftAction(input: {
  values: RequestFormValues;
  transformerId: number;
  distributorId: number;
  requestId?: number;
}): Promise<RequestActionState> {
  const session = await applicantSession();
  if (!session) return NO_SESSION;

  const editing = input.requestId !== undefined;
  const requestId = editing ? parseId(input.requestId) : null;
  if (editing && !requestId) return { status: "error", message: "La solicitud no es válida." };

  // La instaladora indica el titular al crear (B-11); al editar el borrador el titular ya no cambia.
  const parsed = parseRequestForm(input.values, { needsHolder: !editing && session.role === "InstallerCompany" });
  if (!parsed.ok) return { status: "error", message: "Revisa los campos marcados.", fieldErrors: parsed.errors };

  const body = {
    ...parsed.project,
    transformerId: input.transformerId,
    distributorId: input.distributorId,
    ...(parsed.holder ? { holder: parsed.holder } : {}),
  };

  let id: number;
  try {
    if (requestId) {
      await apiData(`/ConnectionRequest/${requestId}`, { method: "PUT", body: { ...body, id: requestId }, token: session.token });
      id = requestId;
    } else {
      id = await apiData<number>("/ConnectionRequest", { method: "POST", body, token: session.token });
    }
  } catch (error) {
    return failure(error);
  }

  revalidatePath("/portal/solicitudes");
  redirect(`/portal/solicitudes/${id}?guardado=1`);
}

const TRANSITIONS = {
  submit: { path: "submit", done: "Solicitud enviada. Edesur la recibió y te avisará por correo de cada cambio." },
  cancel: { path: "cancel", done: "Solicitud cancelada." },
  discard: { path: "cancel", done: "Borrador descartado." },
  "start-correction": { path: "start-correction", done: "Puedes corregir la solicitud: cambia los documentos y reenvíala." },
  resubmit: { path: "resubmit", done: "Solicitud reenviada. Edesur la revisará de nuevo." },
} as const;

type Transition = keyof typeof TRANSITIONS;

/** Cambios de estado que hace el solicitante (RN-12, RN-23, D-4). */
export async function requestTransitionAction(_prev: RequestActionState, formData: FormData): Promise<RequestActionState> {
  const session = await applicantSession();
  if (!session) return NO_SESSION;

  const id = parseId(formData.get("id"));
  const transition = String(formData.get("transition") ?? "");
  if (!id || !Object.hasOwn(TRANSITIONS, transition)) return { status: "error", message: "La acción no es válida." };

  const { path, done } = TRANSITIONS[transition as Transition];
  const comment = String(formData.get("comment") ?? "").trim();
  if (comment.length > 500) return { status: "error", message: "El motivo no puede pasar de 500 caracteres." };

  try {
    await apiData(`/ConnectionRequest/${id}/${path}`, {
      method: "POST",
      body: path === "cancel" ? { id, comment: comment || null } : undefined,
      token: session.token,
    });
  } catch (error) {
    return failure(error);
  }

  revalidatePath(`/portal/solicitudes/${id}`);
  revalidatePath("/portal/solicitudes");
  return { status: "success", message: done };
}

/** Permiso para que el navegador suba el archivo directo a la API (RN-18). */
export type UploadTicketState =
  | { status: "ok"; ticket: string; uploadUrl: string }
  | { status: "error"; message: string };

/**
 * Pide a la API un permiso de un solo uso para subir un documento de este tipo. El archivo no pasa por este servidor:
 * algunas plataformas (Vercel) no aceptan cuerpos de 10 MB. El token de sesión tampoco sale de aquí: el permiso solo
 * sirve para esa subida y vence en 5 minutos.
 */
export async function requestUploadTicketAction(requestId: number, documentType: string): Promise<UploadTicketState> {
  const session = await applicantSession();
  if (!session) return { status: "error", message: NO_SESSION.message! };

  const id = parseId(requestId);
  if (!id || !/^[A-Za-z]{1,40}$/.test(documentType)) return { status: "error", message: "Elige el tipo de documento." };

  try {
    const data = await apiData<{ ticket: string; uploadPath: string }>(`/ConnectionRequest/${id}/documents/upload-ticket`, {
      method: "POST",
      body: { documentType },
      token: session.token,
    });
    const env = serverEnv();
    const uploadUrl = new URL(data.uploadPath, env.ENERGYMAP_PUBLIC_API_URL ?? env.ENERGYMAP_API_URL).toString();
    return { status: "ok", ticket: data.ticket, uploadUrl };
  } catch (error) {
    const failed = failure(error);
    return { status: "error", message: failed.message ?? "No pudimos preparar la subida." };
  }
}

/** Después de subir directo a la API: la página del expediente vuelve a leer los documentos. */
export async function documentUploadedAction(requestId: number): Promise<void> {
  const id = parseId(requestId);
  if (id) revalidatePath(`/portal/solicitudes/${id}`);
}

export async function deleteDocumentAction(_prev: RequestActionState, formData: FormData): Promise<RequestActionState> {
  const session = await applicantSession();
  if (!session) return NO_SESSION;

  const id = parseId(formData.get("id"));
  const documentId = parseId(formData.get("documentId"));
  if (!id || !documentId) return { status: "error", message: "El documento no es válido." };

  try {
    await apiData(`/ConnectionRequest/${id}/documents/${documentId}`, { method: "DELETE", token: session.token });
  } catch (error) {
    return failure(error);
  }

  revalidatePath(`/portal/solicitudes/${id}`);
  return { status: "success", message: "Documento retirado del expediente." };
}
