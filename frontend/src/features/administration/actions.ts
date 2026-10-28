"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { z } from "zod";

import { can } from "@/config/permissions";
import { ApiError, apiData, apiRequest } from "@/server/api-client";
import { getSession } from "@/server/session";

import { DISTRIBUTOR_TYPES, INTERNAL_ROLES, STAFF_ROLES, type InternalRole } from "./api";
import { passwordRule } from "./password";

export type AdminActionState = {
  status: "idle" | "error" | "success";
  message?: string;
  fieldErrors?: Record<string, string[]>;
  /** Lo enviado, salvo las contraseñas, para no vaciar el formulario cuando hay errores. */
  values?: Record<string, string>;
};

const NO_ACCESS: AdminActionState = { status: "error", message: "Solo el administrador gestiona usuarios internos (matriz 3.2)." };

async function adminSession() {
  const session = await getSession();
  return session && can(session.role, "manageInternalUsers") ? session : null;
}

const SECRET = new Set(["password", "confirmPassword"]);
function sent(formData: FormData): Record<string, string> {
  const values: Record<string, string> = {};
  for (const [key, value] of formData.entries()) if (typeof value === "string" && !SECRET.has(key) && !key.startsWith("$")) values[key] = value;
  return values;
}

// Las mismas reglas de RegisterValidator e Identity.
const createSchema = z
  .object({
    fullName: z.string().trim().min(2, "Escribe el nombre completo.").max(150, "Máximo 150 caracteres."),
    email: z.email("Escribe un correo válido, por ejemplo nombre@edesur.com.do.").max(150, "Máximo 150 caracteres."),
    userType: z.enum(INTERNAL_ROLES, "Elige el rol."),
    distributorId: z.string().optional(),
    password: passwordRule,
    confirmPassword: z.string(),
  })
  .refine((v) => v.password === v.confirmPassword, { path: ["confirmPassword"], message: "Las contraseñas no coinciden." })
  // El ingeniero y la distribuidora trabajan para una EDE y solo ven lo de ella.
  .refine((v) => !STAFF_ROLES.includes(v.userType) || positiveId(v.distributorId) !== null, {
    path: ["distributorId"],
    message: "Elige la distribuidora a la que pertenece.",
  });

function positiveId(value: unknown): number | null {
  const n = Number(value);
  return Number.isInteger(n) && n > 0 ? n : null;
}

/** Alta de un usuario interno (§3.2). La persona confirma el correo y después puede cambiar la contraseña. */
export async function createInternalUserAction(_prev: AdminActionState, formData: FormData): Promise<AdminActionState> {
  const session = await adminSession();
  if (!session) return NO_ACCESS;

  const parsed = createSchema.safeParse(Object.fromEntries(formData));
  if (!parsed.success) {
    const fieldErrors: Record<string, string[]> = {};
    for (const issue of parsed.error.issues) fieldErrors[String(issue.path[0])] ??= [issue.message];
    return { status: "error", message: "Revisa los campos marcados.", fieldErrors, values: sent(formData) };
  }

  const { fullName, email, userType, password } = parsed.data;
  const distributorId = STAFF_ROLES.includes(userType as InternalRole) ? positiveId(parsed.data.distributorId) : null;
  try {
    await apiRequest("/Auth/register-internal", {
      method: "POST",
      body: { fullName, email, userType, password, distributorId },
      token: session.token,
    });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    return { status: "error", message: error.errors.join(" ") || error.message, values: sent(formData) };
  }

  revalidatePath("/gestion/usuarios");
  return {
    status: "success",
    message: `Cuenta creada. Enviamos a ${email} un enlace para confirmar el correo; después podrá iniciar sesión con la contraseña temporal.`,
  };
}

/** Desactivar o reactivar (B-5). La API impide desactivar la propia cuenta y al último administrador. */
export async function setUserActiveAction(_prev: AdminActionState, formData: FormData): Promise<AdminActionState> {
  const session = await adminSession();
  if (!session) return NO_ACCESS;

  const id = String(formData.get("id") ?? "");
  const active = formData.get("active") === "true";
  if (!/^[\w-]{1,64}$/.test(id)) return { status: "error", message: "El usuario no es válido." };

  try {
    await apiData(`/Users/${id}/${active ? "reactivate" : "deactivate"}`, { method: "POST", token: session.token });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    return { status: "error", message: error.status === 404 ? "El usuario no existe o no es interno." : error.message };
  }

  revalidatePath("/gestion/usuarios");
  return { status: "success", message: active ? "Cuenta reactivada." : "Cuenta desactivada: ya no puede iniciar sesión." };
}

/** Cambia la EDE de un ingeniero o una distribuidora. La API invalida su sesión: tiene que volver a entrar. */
export async function setUserDistributorAction(_prev: AdminActionState, formData: FormData): Promise<AdminActionState> {
  const session = await adminSession();
  if (!session) return NO_ACCESS;

  const id = String(formData.get("id") ?? "");
  const distributorId = positiveId(formData.get("distributorId"));
  if (!/^[\w-]{1,64}$/.test(id)) return { status: "error", message: "El usuario no es válido." };
  if (!distributorId) return { status: "error", message: "Elige la distribuidora." };

  try {
    await apiData(`/Users/${id}/distributor`, { method: "PUT", body: { distributorId }, token: session.token });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    return { status: "error", message: error.status === 404 ? "El usuario no existe o no es interno." : error.message };
  }

  revalidatePath("/gestion/usuarios");
  return { status: "success", message: "Distribuidora asignada. Tiene que volver a iniciar sesión." };
}

// ---------------- EDE ----------------

const percent = (label: string) =>
  z.coerce
    .number(`Escribe el umbral de ${label} en %.`)
    .gt(0, `El umbral de ${label} debe ser mayor que 0 %.`)
    .max(100, `El umbral de ${label} no puede pasar de 100 %.`);

// Las mismas reglas de DistributorFieldsValidator.
const distributorSchema = z
  .object({
    name: z.string().trim().min(2, "Escribe el nombre.").max(100, "Máximo 100 caracteres."),
    type: z.enum(DISTRIBUTOR_TYPES, "Elige el tipo."),
    contactEmail: z.union([z.literal(""), z.email("Escribe un correo válido.").max(150, "Máximo 150 caracteres.")]),
    contactPhone: z.string().trim().max(20, "Máximo 20 caracteres."),
    availableThresholdPct: percent("disponible"),
    conditionedThresholdPct: percent("condicionada"),
    defaultPowerFactor: z.coerce.number("Escribe el factor de potencia.").min(0.5, "Entre 0,5 y 1.").max(1, "Entre 0,5 y 1."),
  })
  .refine((v) => v.conditionedThresholdPct < v.availableThresholdPct, {
    path: ["conditionedThresholdPct"],
    message: "Tiene que ser menor que el umbral de disponible.",
  });

/** Alta (sin id) o edición de una EDE. Al cambiar los umbrales, la API reclasifica sus transformadores. */
export async function saveDistributorAction(_prev: AdminActionState, formData: FormData): Promise<AdminActionState> {
  const session = await adminSession();
  if (!session) return NO_ACCESS;

  const id = positiveId(formData.get("id"));
  const raw: Record<string, unknown> = Object.fromEntries(formData);
  // Coma o punto decimal, como lo escriba la persona.
  for (const key of ["availableThresholdPct", "conditionedThresholdPct", "defaultPowerFactor"])
    if (typeof raw[key] === "string") raw[key] = (raw[key] as string).trim().replace(",", ".");

  const parsed = distributorSchema.safeParse(raw);
  if (!parsed.success) {
    const fieldErrors: Record<string, string[]> = {};
    for (const issue of parsed.error.issues) fieldErrors[String(issue.path[0])] ??= [issue.message];
    return { status: "error", message: "Revisa los campos marcados.", fieldErrors, values: sent(formData) };
  }

  const body = { ...parsed.data, contactEmail: parsed.data.contactEmail || null, contactPhone: parsed.data.contactPhone || null };
  let savedId: number;
  try {
    const saved = await apiData<{ id: number }>(id ? `/Distributors/manage/${id}` : "/Distributors/manage", {
      method: id ? "PUT" : "POST",
      body,
      token: session.token,
    });
    savedId = saved.id;
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    return { status: "error", message: error.errors.join(" ") || error.message, values: sent(formData) };
  }

  revalidatePath("/gestion/distribuidoras");
  revalidatePath(`/gestion/distribuidoras/${savedId}`);
  if (!id) redirect(`/gestion/distribuidoras/${savedId}?creada=1`);
  return { status: "success", message: "Cambios guardados." };
}

/** Desactivar no borra nada: la EDE deja de ofrecerse y no admite transformadores ni personal nuevos. */
export async function setDistributorActiveAction(_prev: AdminActionState, formData: FormData): Promise<AdminActionState> {
  const session = await adminSession();
  if (!session) return NO_ACCESS;

  const id = positiveId(formData.get("id"));
  const active = formData.get("active") === "true";
  if (!id) return { status: "error", message: "La distribuidora no es válida." };

  try {
    await apiData(`/Distributors/manage/${id}/${active ? "reactivate" : "deactivate"}`, { method: "POST", token: session.token });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    return { status: "error", message: error.status === 404 ? "La distribuidora no existe." : error.message };
  }

  revalidatePath("/gestion/distribuidoras");
  revalidatePath(`/gestion/distribuidoras/${id}`);
  return { status: "success", message: active ? "Distribuidora reactivada." : "Distribuidora desactivada." };
}
