import { z } from "zod";

/**
 * Validación de los formularios de acceso y perfil. Repite las reglas de la API (Identity y FluentValidation)
 * para avisar antes de enviar; la API vuelve a validar.
 */

const email = z.email("Escribe un correo válido, por ejemplo nombre@dominio.com.").max(150, "El correo es demasiado largo.");

// Identity: RequiredLength 10, RequireDigit, RequireUppercase, RequireLowercase.
export const passwordSchema = z
  .string()
  .min(10, "La contraseña debe tener al menos 10 caracteres.")
  .regex(/[A-Z]/, "La contraseña debe tener al menos una mayúscula.")
  .regex(/[a-z]/, "La contraseña debe tener al menos una minúscula.")
  .regex(/\d/, "La contraseña debe tener al menos un número.");

const optionalText = (max: number, message: string) =>
  z
    .string()
    .trim()
    .max(max, message)
    .transform((v) => (v === "" ? undefined : v))
    .optional();

const optionalEmail = z
  .string()
  .trim()
  .transform((v) => (v === "" ? undefined : v))
  .pipe(email.optional());

export const loginSchema = z.object({
  email,
  password: z.string().min(1, "Escribe tu contraseña."),
});

export const registerSchema = z
  .object({
    fullName: z.string().trim().min(2, "Escribe tu nombre completo.").max(150, "El nombre no puede pasar de 150 caracteres."),
    email,
    // Registro público: solo Cliente o Instaladora (RF-06). Los roles internos los crea el administrador.
    userType: z.enum(["Customer", "InstallerCompany"], "Elige si te registras como cliente o como empresa instaladora."),
    password: passwordSchema,
    confirmPassword: z.string(),
  })
  .refine((v) => v.password === v.confirmPassword, { path: ["confirmPassword"], message: "Las contraseñas no coinciden." });

export const forgotPasswordSchema = z.object({ email });

export const resetPasswordSchema = z
  .object({
    email,
    code: z.string().min(1, "El enlace no es válido. Pide uno nuevo."),
    password: passwordSchema,
    confirmPassword: z.string(),
  })
  .refine((v) => v.password === v.confirmPassword, { path: ["confirmPassword"], message: "Las contraseñas no coinciden." });

// CreateMyCustomerProfileValidator
export const customerProfileSchema = z.object({
  fullName: z.string().trim().min(1, "Escribe el nombre del titular.").max(150, "Máximo 150 caracteres."),
  nationalIdOrRnc: z.string().trim().min(1, "Escribe la cédula o el RNC.").max(20, "Máximo 20 caracteres."),
  isLegalEntity: z.boolean(),
  email: optionalEmail,
  phone: optionalText(30, "Máximo 30 caracteres."),
  address: optionalText(250, "Máximo 250 caracteres."),
});

// RegisterInstallerCompanyValidator
export const installerCompanySchema = z.object({
  tradeName: z.string().trim().min(1, "Escribe el nombre comercial.").max(150, "Máximo 150 caracteres."),
  rnc: z.string().trim().min(1, "Escribe el RNC de la empresa.").max(20, "Máximo 20 caracteres."),
  contactEmail: optionalEmail,
  contactPhone: optionalText(20, "Máximo 20 caracteres."),
  address: optionalText(250, "Máximo 250 caracteres."),
});

/** Resultado de un formulario enviado con una Server Action. */
export type FormState = {
  status: "idle" | "error" | "success";
  message?: string;
  fieldErrors?: Record<string, string[] | undefined>;
  /** Valores enviados, para no vaciar el formulario cuando hay errores (las contraseñas nunca vuelven). */
  values?: Record<string, string>;
};

export const INITIAL_FORM_STATE: FormState = { status: "idle" };
