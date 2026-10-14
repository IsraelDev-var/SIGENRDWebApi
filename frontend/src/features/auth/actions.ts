"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import type { z } from "zod";

import { canEnterZone, homeFor, zoneOf } from "@/config/permissions";
import type { components } from "@/lib/api/schema";
import { safeRedirectPath } from "@/lib/safe-redirect";
import { ApiError, apiData, apiRequest } from "@/server/api-client";
import { createSession, deleteSession, getSession } from "@/server/session";

import {
  customerProfileSchema,
  forgotPasswordSchema,
  installerCompanySchema,
  loginSchema,
  registerSchema,
  resetPasswordSchema,
  type FormState,
} from "./schemas";

type AuthResponse = components["schemas"]["AuthResponse"];

const SECRET_FIELDS = new Set(["password", "confirmPassword"]);

function formValues(formData: FormData): Record<string, string> {
  const values: Record<string, string> = {};
  for (const [key, value] of formData.entries()) {
    if (typeof value === "string" && !SECRET_FIELDS.has(key) && !key.startsWith("$")) values[key] = value;
  }
  return values;
}

function invalid(error: z.ZodError, formData: FormData): FormState {
  // Un error por campo, el primero: con la contraseña vacía no se listan las cuatro reglas a la vez
  // (ya están en la ayuda del campo).
  const fieldErrors: Record<string, string[]> = {};
  for (const issue of error.issues) {
    const key = String(issue.path[0] ?? "form");
    fieldErrors[key] ??= [issue.message];
  }
  return { status: "error", message: "Revisa los campos marcados.", fieldErrors, values: formValues(formData) };
}

function failed(error: unknown, formData: FormData, fallback?: string): FormState {
  if (error instanceof ApiError) {
    const message = error.errors.length > 0 ? error.errors.join(" ") : (fallback ?? error.message);
    return { status: "error", message, values: formValues(formData) };
  }
  throw error;
}

export async function loginAction(_prev: FormState, formData: FormData): Promise<FormState> {
  const parsed = loginSchema.safeParse({ email: formData.get("email"), password: formData.get("password") });
  if (!parsed.success) return invalid(parsed.error, formData);

  let response: AuthResponse;
  try {
    response = await apiRequest<AuthResponse>("/Auth/login", { method: "POST", body: parsed.data });
  } catch (error) {
    // 429: cuenta bloqueada por intentos fallidos; la API dice cuánto esperar.
    // 403: contraseña correcta, pero un administrador desactivó la cuenta (B-5); la API da el motivo.
    if (error instanceof ApiError && (error.status === 429 || error.status === 403)) return failed(error, formData);
    // Mismo mensaje para correo inexistente, contraseña errónea o cuenta sin confirmar: no se revela cuál fue.
    return failed(error, formData, "El correo o la contraseña no son correctos, o la cuenta todavía no está confirmada.");
  }

  const user = response.token ? await createSession(response.token) : null;
  if (!user) {
    return { status: "error", message: "No pudimos iniciar la sesión. Inténtalo de nuevo.", values: formValues(formData) };
  }

  // Vuelve a la página pedida solo si es interna y el rol puede entrar en ella.
  const home = homeFor(user.role);
  const next = safeRedirectPath(formData.get("next"), home);
  const zone = zoneOf(next.split("?")[0]!);
  redirect(zone && !canEnterZone(user.role, zone) ? home : next);
}

export async function registerAction(_prev: FormState, formData: FormData): Promise<FormState> {
  const parsed = registerSchema.safeParse({
    fullName: formData.get("fullName"),
    email: formData.get("email"),
    userType: formData.get("userType"),
    password: formData.get("password"),
    confirmPassword: formData.get("confirmPassword"),
  });
  if (!parsed.success) return invalid(parsed.error, formData);

  const { fullName, email, password, userType } = parsed.data;
  try {
    await apiRequest<AuthResponse>("/Auth/register", { method: "POST", body: { fullName, email, password, userType } });
  } catch (error) {
    return failed(error, formData);
  }

  // La API responde igual exista o no la cuenta (SEC-08): el mensaje tampoco lo revela.
  return {
    status: "success",
    message: `Si los datos son correctos, enviamos un enlace a ${parsed.data.email} para confirmar la cuenta. Ábrelo y después inicia sesión.`,
  };
}

export async function confirmEmailAction(_prev: FormState, formData: FormData): Promise<FormState> {
  const userId = String(formData.get("userId") ?? "");
  const code = String(formData.get("code") ?? "");
  if (!userId || !code) return { status: "error", message: "El enlace está incompleto. Ábrelo de nuevo desde el correo." };

  try {
    const query = new URLSearchParams({ userId, code });
    const result = await apiRequest<AuthResponse>(`/Auth/confirm-email?${query}`);
    return { status: "success", message: result.message ?? "Correo confirmado. Ya puedes iniciar sesión." };
  } catch (error) {
    return failed(error, formData);
  }
}

export async function forgotPasswordAction(_prev: FormState, formData: FormData): Promise<FormState> {
  const parsed = forgotPasswordSchema.safeParse({ email: formData.get("email") });
  if (!parsed.success) return invalid(parsed.error, formData);

  try {
    await apiRequest<AuthResponse>("/Auth/forgot-password", { method: "POST", body: parsed.data });
  } catch (error) {
    return failed(error, formData);
  }

  return {
    status: "success",
    message: `Si ${parsed.data.email} tiene una cuenta confirmada, te enviamos un enlace para crear una contraseña nueva.`,
  };
}

export async function resetPasswordAction(_prev: FormState, formData: FormData): Promise<FormState> {
  const parsed = resetPasswordSchema.safeParse({
    email: formData.get("email"),
    code: formData.get("code"),
    password: formData.get("password"),
    confirmPassword: formData.get("confirmPassword"),
  });
  if (!parsed.success) return invalid(parsed.error, formData);

  try {
    await apiRequest<AuthResponse>("/Auth/reset-password", {
      method: "POST",
      body: { email: parsed.data.email, code: parsed.data.code, newPassword: parsed.data.password },
    });
  } catch (error) {
    return failed(error, formData);
  }

  return { status: "success", message: "Listo. Ya puedes iniciar sesión con la contraseña nueva." };
}

export async function logoutAction(): Promise<void> {
  // Los JWT no se revocan en la API (60 min): cerrar sesión borra la cookie, así el navegador deja de tenerlo.
  await deleteSession();
  redirect("/");
}

export async function createCustomerProfileAction(_prev: FormState, formData: FormData): Promise<FormState> {
  const session = await getSession();
  if (!session || session.role !== "Customer") redirect("/ingresar?next=/portal/perfil");

  const parsed = customerProfileSchema.safeParse({
    fullName: formData.get("fullName"),
    nationalIdOrRnc: formData.get("nationalIdOrRnc"),
    isLegalEntity: formData.get("isLegalEntity") === "on",
    email: formData.get("email") ?? "",
    phone: formData.get("phone") ?? "",
    address: formData.get("address") ?? "",
  });
  if (!parsed.success) return invalid(parsed.error, formData);

  try {
    await apiData("/Customers/me", { method: "POST", body: parsed.data, token: session.token });
  } catch (error) {
    return failed(error, formData);
  }

  revalidatePath("/portal", "layout");
  redirect("/portal");
}

export async function registerInstallerCompanyAction(_prev: FormState, formData: FormData): Promise<FormState> {
  const session = await getSession();
  if (!session || session.role !== "InstallerCompany") redirect("/ingresar?next=/portal/perfil");

  const parsed = installerCompanySchema.safeParse({
    tradeName: formData.get("tradeName"),
    rnc: formData.get("rnc"),
    contactEmail: formData.get("contactEmail") ?? "",
    contactPhone: formData.get("contactPhone") ?? "",
    address: formData.get("address") ?? "",
  });
  if (!parsed.success) return invalid(parsed.error, formData);

  try {
    await apiData("/InstallerCompanies/me", { method: "POST", body: parsed.data, token: session.token });
  } catch (error) {
    return failed(error, formData);
  }

  revalidatePath("/portal", "layout");
  redirect("/portal");
}
