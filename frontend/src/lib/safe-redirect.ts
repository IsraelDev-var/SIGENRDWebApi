/**
 * Destino tras iniciar sesión (?next=). Solo rutas internas: "/portal" sí; "//sitio.com", "https://…" o "/\\sitio"
 * no, para que el enlace de inicio de sesión no sirva para redirigir a otro sitio (open redirect).
 */
export function safeRedirectPath(value: unknown, fallback: string): string {
  if (typeof value !== "string" || value.length === 0 || value.length > 512) return fallback;
  if (!value.startsWith("/") || value.startsWith("//") || value.includes("\\")) return fallback;
  // Caracteres de control: algunos navegadores los ignoran y convierten "/\t/sitio" en "//sitio".
  if (/[\u0000-\u001f\u007f]/.test(value)) return fallback;
  return value;
}
