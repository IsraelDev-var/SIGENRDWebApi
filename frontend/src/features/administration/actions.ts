"use server";

import { revalidatePath } from "next/cache";
import { z } from "zod";

import { can } from "@/config/permissions";
import { ApiError, apiData, apiRequest } from "@/server/api-client";
import { getSession } from "@/server/session";

import { INTERNAL_ROLES } from "./api";
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
    password: passwordRule,
    confirmPassword: z.string(),
  })
  .refine((v) => v.password === v.confirmPassword, { path: ["confirmPassword"], message: "Las contraseñas no coinciden." });

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
  try {
    await apiRequest("/Auth/register-internal", { method: "POST", body: { fullName, email, userType, password }, token: session.token });
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
