import { z } from "zod";

// Una variable vacía en .env cuenta como no definida.
const optionalUrl = z.preprocess((value) => (value === "" ? undefined : value), z.url().optional());

export const publicEnvSchema = z.object({
  /** Estilos del mapa base. Sin definir, se usan los de CARTO: solo para desarrollo (decisión D-2). */
  NEXT_PUBLIC_MAP_STYLE_LIGHT: optionalUrl,
  NEXT_PUBLIC_MAP_STYLE_DARK: optionalUrl,
});

export type PublicEnv = z.infer<typeof publicEnvSchema>;

// Next solo sustituye las variables NEXT_PUBLIC_* escritas literalmente: no se puede pasar process.env entero.
export const publicEnv: PublicEnv = publicEnvSchema.parse({
  NEXT_PUBLIC_MAP_STYLE_LIGHT: process.env.NEXT_PUBLIC_MAP_STYLE_LIGHT,
  NEXT_PUBLIC_MAP_STYLE_DARK: process.env.NEXT_PUBLIC_MAP_STYLE_DARK,
});
