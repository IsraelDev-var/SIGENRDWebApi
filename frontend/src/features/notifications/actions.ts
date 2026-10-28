"use server";

import { revalidatePath } from "next/cache";

import { ApiError, apiData } from "@/server/api-client";
import { getSession } from "@/server/session";

export type NotificationActionState = { status: "idle" | "error"; message?: string };

/** Marca una notificación como leída, o todas si no se indica cuál (RF-32). Solo las propias: la API responde 404 a las ajenas. */
export async function markReadAction(_prev: NotificationActionState, formData: FormData): Promise<NotificationActionState> {
  const session = await getSession();
  if (!session) return { status: "error", message: "Tu sesión terminó. Vuelve a ingresar." };

  const id = formData.get("id");
  const path = id === null ? "/Notifications/read-all" : /^\d{1,10}$/.test(String(id)) ? `/Notifications/${id}/read` : null;
  if (!path) return { status: "error", message: "La notificación no es válida." };

  try {
    await apiData(path, { method: "POST", token: session.token });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    return { status: "error", message: error.status === 404 ? "La notificación ya no existe." : error.message };
  }

  revalidatePath("/portal/notificaciones");
  revalidatePath("/gestion/notificaciones");
  return { status: "idle" };
}
