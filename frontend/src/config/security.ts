/**
 * Content Security Policy del frontend (F9, endurecimiento). Una función pura para poder probarla: proxy.ts genera un
 * nonce por petición y Next lo pone en sus propios scripts (todas las páginas se renderizan por petición).
 */

/** Origen del mapa base de CARTO (estilo, teselas, sprites y fuentes), el de desarrollo (D-2). */
export const MAP_ORIGINS = ["https://basemaps.cartocdn.com", "https://*.basemaps.cartocdn.com"];

type Options = {
  /** Desarrollo: React usa eval para sus mensajes de error y Next abre un websocket para recargar. */
  dev: boolean;
  /** Solo por HTTPS se pide al navegador que suba a HTTPS lo que pida por HTTP. */
  https: boolean;
  /**
   * URL de los estilos del mapa en uso (NEXT_PUBLIC_MAP_STYLE_*). Su origen se permite junto al de CARTO, para que un
   * proveedor de producción (MapTiler, PMTiles propios) funcione sin tocar este archivo si sirve todo desde ese origen.
   */
  mapStyles?: string[];
};

function mapOrigins(mapStyles: string[]): string[] {
  return [...new Set([...MAP_ORIGINS, ...mapStyles.map((url) => new URL(url).origin)])];
}

export function contentSecurityPolicy(nonce: string, { dev, https, mapStyles = [] }: Options): string {
  const map = mapOrigins(mapStyles);
  const directives: Record<string, string[]> = {
    "default-src": ["'self'"],
    // 'strict-dynamic': lo que cargue un script con el nonce también vale (los chunks de Next). Sin 'unsafe-inline'.
    "script-src": ["'self'", `'nonce-${nonce}'`, "'strict-dynamic'", ...(dev ? ["'unsafe-eval'"] : [])],
    // Los atributos style de MapLibre y Recharts no admiten nonce: estilos en línea permitidos (no ejecutan código).
    "style-src": ["'self'", "'unsafe-inline'"],
    "img-src": ["'self'", "data:", "blob:", ...map],
    "font-src": ["'self'", "data:"],
    "connect-src": ["'self'", ...map, ...(dev ? ["ws:", "wss:"] : [])],
    // El worker de MapLibre se sirve desde nuestro origen; MapLibre también crea workers desde blob:.
    "worker-src": ["'self'", "blob:"],
    "object-src": ["'none'"],
    "base-uri": ["'self'"],
    "form-action": ["'self'"],
    "frame-ancestors": ["'none'"],
  };

  const policy = Object.entries(directives).map(([name, values]) => `${name} ${values.join(" ")}`);
  if (https) policy.push("upgrade-insecure-requests");
  return policy.join("; ");
}

/** Nonce de 128 bits en base64, nuevo en cada petición. */
export function newNonce(): string {
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  return btoa(String.fromCharCode(...bytes));
}
