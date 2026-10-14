"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { can } from "@/config/permissions";
import { ApiError, apiData } from "@/server/api-client";
import { getSession } from "@/server/session";

import { parseSimulationForm, type FieldErrors, type SimulationFormValues } from "./schemas";

export type SimulationActionState = { status: "idle" | "error"; message?: string; fieldErrors?: FieldErrors };

/** Pide el cálculo a la API (RF-18 a RF-25) y lleva al resultado, que queda en el historial (RF-26). */
export async function createSimulationAction(values: SimulationFormValues): Promise<SimulationActionState> {
  const session = await getSession();
  if (!session || !can(session.role, "manageOwnSimulations")) {
    return { status: "error", message: "Tu sesión terminó. Vuelve a iniciar sesión para continuar." };
  }

  const parsed = parseSimulationForm(values);
  if (!parsed.ok) return { status: "error", message: "Revisa los campos marcados.", fieldErrors: parsed.errors };

  let id: number;
  try {
    id = (await apiData<{ id: number }>("/Simulations", { method: "POST", body: parsed.body, token: session.token })).id;
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    return { status: "error", message: error.errors.length > 0 ? error.errors.join(" ") : error.message };
  }

  revalidatePath("/portal/simulaciones");
  redirect(`/portal/simulaciones/${id}`);
}
