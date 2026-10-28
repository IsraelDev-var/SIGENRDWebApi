import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Sin la cabecera X-Powered-By, igual que la API, que no anuncia su servidor.
  poweredByHeader: false,
  // Cabeceras de seguridad de todas las respuestas (F9). La Content Security Policy, con nonce, la pone proxy.ts.
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-Frame-Options", value: "DENY" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          // Solo la ubicación, y solo desde nuestro origen ("Mi ubicación" del mapa).
          { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=(self), payment=(), usb=()" },
          { key: "Cross-Origin-Opener-Policy", value: "same-origin" },
          // Otro sitio no puede cargar nuestros recursos (aislamiento contra Spectre, aviso 90004 de ZAP).
          { key: "Cross-Origin-Resource-Policy", value: "same-origin" },
          // HSTS solo en producción: en desarrollo y en las pruebas se usa http://localhost.
          ...(process.env.NODE_ENV === "production"
            ? [{ key: "Strict-Transport-Security", value: "max-age=31536000; includeSubDomains" }]
            : []),
        ],
      },
    ];
  },
  experimental: {
    // Los documentos de una solicitud pesan hasta 10 MB (RN-18) y se suben con una Server Action. Se deja margen para
    // el resto del formulario multipart. proxy.ts copia el cuerpo de las rutas /portal, así que su límite es el mismo.
    serverActions: { bodySizeLimit: "11mb" },
    proxyClientMaxBodySize: "11mb",
  },
};

export default nextConfig;
