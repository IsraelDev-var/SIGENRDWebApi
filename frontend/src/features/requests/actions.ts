"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { can } from "@/config/permissions";
import { ApiError, apiData } from "@/server/api-client";
import { getSession, type Session } from "@/server/session";

import { documentFileError, parseRequestForm, type FieldErrors, type RequestFormValues } from "./schemas";

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

/** Sube un documento del expediente (RN-18). Se revisa antes de enviarlo para no hacer esperar 10 MB en vano. */
export async function uploadDocumentAction(_prev: RequestActionState, formData: FormData): Promise<RequestActionState> {
  const session = await applicantSession();
  if (!session) return NO_SESSION;

  const id = parseId(formData.get("id"));
  const documentType = String(formData.get("documentType") ?? "");
  const file = formData.get("file");
  if (!id || !/^[A-Za-z]{1,40}$/.test(documentType)) return { status: "error", message: "Elige el tipo de documento." };

  const fileError = documentFileError(file instanceof File ? file : null);
  if (fileError) return { status: "error", message: fileError, fieldErrors: { file: [fileError] } };

  const upload = new FormData();
  upload.set("File", file as File);
  upload.set("DocumentType", documentType);

  try {
    await apiData(`/ConnectionRequest/${id}/documents`, { method: "POST", body: upload, token: session.token });
  } catch (error) {
    return failure(error);
  }

  revalidatePath(`/portal/solicitudes/${id}`);
  return { status: "success", message: `Subimos ${(file as File).name}.` };
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
