import { z } from "zod";

/** Reglas de Identity para la contraseña (RequiredLength 10, mayúscula, minúscula y número), como en el registro. */
export const passwordRule = z
  .string()
  .min(10, "La contraseña debe tener al menos 10 caracteres.")
  .regex(/[A-Z]/, "La contraseña debe tener al menos una mayúscula.")
  .regex(/[a-z]/, "La contraseña debe tener al menos una minúscula.")
  .regex(/\d/, "La contraseña debe tener al menos un número.");
